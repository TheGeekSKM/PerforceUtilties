import subprocess
import os
import datetime

class P4Wrapper:
    P4PORT = ""
    P4USER = ""
    
    # Mock Settings
    MOCK_MODE = True
    MOCK_LOG_PATH = "p4_mock_log.txt"

    @staticmethod
    def LogMockEvent(message):
        """Allows external scripts to write headlines to the mock log."""
        try:
            with open(P4Wrapper.MOCK_LOG_PATH, "a") as f:
                f.write(f"\n{message}\n")
        except: pass

    @staticmethod
    def ExecuteRealCommand(arguments, input_data=None):
        """Helper to run the actual subprocess command."""
        env = os.environ.copy()
        if P4Wrapper.P4PORT: env["P4PORT"] = P4Wrapper.P4PORT
        if P4Wrapper.P4USER: env["P4USER"] = P4Wrapper.P4USER

        startupinfo = None
        if os.name == 'nt':
            startupinfo = subprocess.STARTUPINFO()
            startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW

        try:
            cmd = ["p4"] + arguments.split()
            process = subprocess.Popen(
                cmd,
                stdin=subprocess.PIPE if input_data else None,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                env=env,
                shell=False, 
                startupinfo=startupinfo
            )
            
            stdout, stderr = process.communicate(input=input_data)
            return stdout, stderr
        except FileNotFoundError:
             return "", "Error: 'p4' command not found."
        except Exception as e:
            return "", str(e)

    @staticmethod
    def RunCommand(arguments, input_data=None):
        log_entry = f"Command: p4 {arguments}\n"
        if input_data:
            log_entry += f"Input Data:\n{input_data}\n"
        log_entry += "-" * 20 + "\n"

        try:
            with open(P4Wrapper.MOCK_LOG_PATH, "a") as f:
                f.write(log_entry)
        except: 
            pass

        if P4Wrapper.MOCK_MODE:
            if "group -o" in arguments or "users" in arguments or "login" in arguments:
                return P4Wrapper.ExecuteRealCommand(arguments, input_data)
            
            return "Mock Command Success (Logged Only)", ""
        
        return P4Wrapper.ExecuteRealCommand(arguments, input_data)
    
    @staticmethod
    def AddUserToGroup(username, group_name):
        # Fetch current group spec
        out, err = P4Wrapper.RunCommand(f"group -o {group_name}")
        if err and "group not found" not in err.lower(): 
            return False, f"Fetch Error: {err}"

        # Check if user is already in it
        if f"\t{username}" in out or f" {username}" in out:
            return True, "User already in group."

        # Modify the spec text
        if "Users:" in out:
            lines = out.splitlines()
            new_lines = []
            in_users = False
            added = False
            for line in lines:
                new_lines.append(line)
                if line.startswith("Users:"):
                    in_users = True
                elif in_users and line.strip() == "":
                    new_lines.insert(-1, f"\t{username}")
                    added = True
                    in_users = False
            
            if not added:
                new_lines.append(f"\t{username}")
            
            new_spec = "\n".join(new_lines)
        else:
            new_spec = out + f"\nUsers:\n\t{username}\n"

        # Save the modified spec
        o, e = P4Wrapper.RunCommand("group -i", input_data=new_spec)
        if e: return False, f"Update Error: {e}"
        return True, f"User {username} added to {group_name}"
    
    @staticmethod
    def SetPassword(username, password):
        # Correct syntax is 'p4 passwd username' (no -u flag)
        return P4Wrapper.RunCommand(f"passwd {username}", input_data=f"{password}\n{password}\n")