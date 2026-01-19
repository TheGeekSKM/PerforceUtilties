import customtkinter as ctk
from tkinter import filedialog
import os
import random
import csv
import sys
import subprocess
from backend.p4 import P4Wrapper
from backend.excel_tools import ConvertExcelToCSV

class BaseScreen(ctk.CTkFrame):
    def __init__(self, parent, controller):
        super().__init__(parent)
        self.controller = controller
        self.SetupUI()
    def SetupUI(self): pass

class LoginScreen(BaseScreen):
    def SetupUI(self):
        
        self.mode_banner = ctk.CTkFrame(self, height=40, corner_radius=0)
        self.mode_banner.pack(fill="x", side="top")
        self.lbl_mode = ctk.CTkLabel(self.mode_banner, text="INITIALIZING...", font=("Arial", 14, "bold"))
        self.lbl_mode.place(relx=0.5, rely=0.5, anchor="center")
        
        ctk.CTkButton(self, text="← Back", width=80, command=lambda: self.controller.show_screen("MainMenu")).pack(anchor="w", padx=20, pady=20)
        container = ctk.CTkFrame(self, fg_color="transparent")
        container.place(relx=0.5, rely=0.5, anchor="center")
        ctk.CTkLabel(container, text="Perforce Login", font=("Arial", 26, "bold")).pack(pady=30)
        self.entry_server = ctk.CTkEntry(container, placeholder_text="Server (e.g., ssl:server:1666)", width=320, height=40)
        self.entry_server.pack(pady=10)
        self.entry_user = ctk.CTkEntry(container, placeholder_text="Admin Username", width=320, height=40)
        self.entry_user.pack(pady=10)
        self.entry_pass = ctk.CTkEntry(container, placeholder_text="Password", show="*", width=320, height=40)
        self.entry_pass.pack(pady=10)

        ctk.CTkButton(container, text="Log In", command=self.AttemptLogin, width=320, height=50, font=("Arial", 16, "bold")).pack(pady=20)
        self.lbl_status = ctk.CTkLabel(container, text="", text_color="#ff5555")
        self.lbl_status.pack()
        
        self.AutofillCredentials()
        
    def AutofillCredentials(self):
        try:
            startupinfo = None
            if os.name == 'nt':
                startupinfo = subprocess.STARTUPINFO()
                startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW

            process = subprocess.Popen(
                ["p4", "set"], 
                stdout=subprocess.PIPE, 
                stderr=subprocess.PIPE, 
                text=True, 
                shell=False,
                startupinfo=startupinfo
            )
            stdout, _ = process.communicate()

            p4_port = ""
            p4_user = ""

            for line in stdout.splitlines():
                if line.startswith("P4PORT="):
                    p4_port = line.split("=", 1)[1].split(" ")[0]
                elif line.startswith("P4USER="):
                    p4_user = line.split("=", 1)[1].split(" ")[0]

            if p4_port:
                self.entry_server.delete(0, "end")
                self.entry_server.insert(0, p4_port)
            
            if p4_user:
                self.entry_user.delete(0, "end")
                self.entry_user.insert(0, p4_user)
                
            if p4_user and p4_port:
                self.mode_banner.configure(fg_color="#2CC985") 
                self.lbl_mode.configure(text="Logged In!", text_color="black")
            else:
                self.mode_banner.configure(fg_color="#FF4747") 
                self.lbl_mode.configure(text="Not Logged In", text_color="white")

        except Exception as e:
            print(f"Autofill failed: {e}")
            self.mode_banner.configure(fg_color="#FF4747") 
            self.lbl_mode.configure(text="Not Logged In", text_color="white")

    def AttemptLogin(self):
        if self.chk_mock.get() == 1:
            P4Wrapper.MOCK_MODE = True
            P4Wrapper.MOCK_LOG_PATH = os.path.join(os.getcwd(), "p4_mock_log.txt")
            with open(P4Wrapper.MOCK_LOG_PATH, "w") as f: f.write("--- STARTED MOCK SESSION ---\n")
            self.controller.show_screen("MainMenu")
            return

        P4Wrapper.MOCK_MODE = False
        s, u, p = self.entry_server.get(), self.entry_user.get(), self.entry_pass.get()
        if not s or not u or not p:
            self.lbl_status.configure(text="Missing credentials.")
            return
        P4Wrapper.P4PORT = s
        P4Wrapper.P4USER = u
        out, err = P4Wrapper.RunCommand("login", input_data=p + "\n")
        if "logged in" in out:
            self.controller.show_screen("MainMenu")
        else:
            self.lbl_status.configure(text=f"Error: {err or 'Login failed'}")

class MainMenuScreen(BaseScreen):
    def SetupUI(self):
        self.mode_banner = ctk.CTkFrame(self, height=40, corner_radius=0)
        self.mode_banner.pack(fill="x", side="top")
        self.lbl_mode = ctk.CTkLabel(self.mode_banner, text="INITIALIZING...", font=("Arial", 14, "bold"))
        self.lbl_mode.place(relx=0.5, rely=0.5, anchor="center")
        
        ctk.CTkLabel(self, text="Perforce Management", font=("Arial", 32, "bold")).pack(pady=(40, 20))
        btn_conf = {"width": 400, "height": 70, "font": ("Arial", 18)}
        ctk.CTkButton(self, text="Login and Settings", command=lambda: self.controller.show_screen("Login"), **btn_conf).pack(pady=15)
        ctk.CTkButton(self, text="Import Excel Roster", command=lambda: self.controller.show_screen("Importer"), **btn_conf).pack(pady=15)
        ctk.CTkButton(self, text="Create Users", command=lambda: self.controller.show_screen("Creator"), **btn_conf).pack(pady=15)
        ctk.CTkButton(self, text="Settings & Admin", command=lambda: self.controller.show_screen("Settings"), **btn_conf).pack(pady=15)
        ctk.CTkButton(self, text="Exit", command=self.controller.quit, fg_color="transparent", border_width=2, text_color=("gray10", "#DCE4EE"), width=200).pack(pady=60)
        
    def RefreshUI(self):
        if P4Wrapper.MOCK_MODE:
            self.mode_banner.configure(fg_color="#2CC985") 
            self.lbl_mode.configure(text="SYSTEM STATUS: MOCK MODE ACTIVE (SAFE)", text_color="black")
        else:
            self.mode_banner.configure(fg_color="#FF4747") 
            self.lbl_mode.configure(text="SYSTEM STATUS: LIVE MODE (DANGER)", text_color="white")

class SettingsScreen(BaseScreen):
    def SetupUI(self):
        ctk.CTkButton(self, text="← Back", width=80, command=lambda: self.controller.show_screen("MainMenu")).pack(anchor="w", padx=20, pady=20)
        
        ctk.CTkLabel(self, text="Settings", font=("Arial", 28, "bold")).pack(pady=10)

        mode_frame = ctk.CTkFrame(self)
        mode_frame.pack(pady=20, padx=40, fill="x")
        
        ctk.CTkLabel(mode_frame, text="System Mode", font=("Arial", 18, "bold")).pack(pady=(20,5))
        
        self.switch_var = ctk.StringVar(value="on" if P4Wrapper.MOCK_MODE else "off")
        self.mode_switch = ctk.CTkSwitch(mode_frame, text="Mock Mode Enabled", command=self.ToggleMode,
                                         variable=self.switch_var, onvalue="on", offvalue="off", font=("Arial", 16))
        self.mode_switch.pack(pady=20)
        
        self.lbl_mode_info = ctk.CTkLabel(mode_frame, text="Mock Mode writes to logs only.\nDisabling this requires an admin password.", text_color="gray")
        self.lbl_mode_info.pack(pady=(0, 20))

        # admin_frame = ctk.CTkFrame(self, fg_color=("gray85", "gray20"))
        # admin_frame.pack(pady=20, padx=40, fill="x")
        
        # ctk.CTkLabel(admin_frame, text="Danger Zone", font=("Arial", 18, "bold"), text_color="#FF4747").pack(pady=(20, 10))
        
        # ctk.CTkButton(admin_frame, text="🗑️ Delete Users Tool", 
        #               command=lambda: self.controller.show_screen("Deleter"),
        #               fg_color="#FF4747", hover_color="#D63030", width=300, height=50).pack(pady=20)

    def RefreshUI(self):
        current_state = "on" if P4Wrapper.MOCK_MODE else "off"
        if self.switch_var.get() != current_state:
            self.switch_var.set(current_state)

    def ToggleMode(self):
        new_val = self.switch_var.get()
        
        if new_val == "off": 
            dialog = ctk.CTkInputDialog(text="Enter Admin Password to enable LIVE MODE:", title="Security Check")
            password = dialog.get_input()
            
            # HARDCODED PASSWORD: "admin"
            if password == "admin":
                P4Wrapper.MOCK_MODE = False
                self.lbl_mode_info.configure(text="!!LIVE MODE ACTIVE!!\nCommands will affect the real server.", text_color="#FF4747")
            else:
                self.switch_var.set("on")
        else:
            P4Wrapper.MOCK_MODE = True
            self.lbl_mode_info.configure(text="Mock Mode writes to logs only.\nDisabling this requires an admin password.", text_color="gray")

class ImporterScreen(BaseScreen):
    def SetupUI(self):
        ctk.CTkButton(self, text="← Back", width=80, command=lambda: self.controller.show_screen("MainMenu")).pack(anchor="w", padx=20, pady=20)
        ctk.CTkLabel(self, text="Excel Importer", font=("Arial", 24, "bold")).pack(pady=20)
        self.btn_select = ctk.CTkButton(self, text="Select .xlsx File", command=self.SelectFile, width=300, height=50)
        self.btn_select.pack(pady=40)
        self.lbl_file = ctk.CTkLabel(self, text="No file selected", text_color="gray")
        self.lbl_file.pack(pady=10)
        self.logger = self.controller.create_logger(self)
        self.logger.pack(fill="both", expand=True, padx=20, pady=20)
    def SelectFile(self):
        path = filedialog.askopenfilename(filetypes=[("Excel", "*.xlsx")])
        if path:
            self.lbl_file.configure(text=os.path.basename(path))
            self.logger.Log(f"Selected: {path}")
            csv_out = os.path.join(os.getcwd(), "perforce_users.csv")
            ok, msg = ConvertExcelToCSV(path, csv_out)
            self.logger.Log(msg, is_error=not ok)

class CreatorScreen(BaseScreen):
    def SetupUI(self):
        ctk.CTkButton(self, text="← Back", width=80, command=lambda: self.controller.show_screen("MainMenu")).pack(anchor="w", padx=20, pady=20)
        ctk.CTkLabel(self, text="User Creator", font=("Arial", 24, "bold")).pack(pady=5)
        self.entry_group = ctk.CTkEntry(self, placeholder_text="Perforce Group Name", width=400)
        self.entry_group.pack(pady=10)
        ctk.CTkButton(self, text="Load CSV Data", command=self.LoadCSV).pack(pady=10)
        self.edit_frame = ctk.CTkFrame(self)
        self.edit_frame.pack(fill="x", padx=40, pady=10)
        self.inputs = {}
        for f in ["Username", "Full Name", "Email", "Password"]:
            row = ctk.CTkFrame(self.edit_frame, fg_color="transparent")
            row.pack(fill="x", padx=10, pady=5)
            ctk.CTkLabel(row, text=f, width=100, anchor="w").pack(side="left")
            e = ctk.CTkEntry(row)
            e.pack(side="right", fill="x", expand=True)
            self.inputs[f] = e
        act_frame = ctk.CTkFrame(self, fg_color="transparent")
        act_frame.pack(pady=10)
        ctk.CTkButton(act_frame, text="Edit", command=self.UnlockUI, fg_color="orange", width=80).pack(side="left", padx=5)
        ctk.CTkButton(act_frame, text="Skip", command=self.Skip, fg_color="gray", width=80).pack(side="left", padx=5)
        ctk.CTkButton(act_frame, text="Continue", command=self.Process, fg_color="green", width=120).pack(side="left", padx=5)
        self.logger = self.controller.create_logger(self)
        self.logger.pack(fill="both", expand=True, padx=20, pady=20)
        self.users = []
        self.created_log = [] 
        self.idx = -1
        self.LockUI()
        
    def Process(self):
        if self.idx >= len(self.users): return
        grp = self.entry_group.get()
        if not grp: 
            self.logger.Log("Group name required!", True); return
        un = self.inputs["Username"].get()
        
        P4Wrapper.LogMockEvent(f"Creating user {un}...")
        self.logger.Log(f"Creating {un}...")

        # 1. Create User (Added -f to force creation if admin)
        spec = f"User: {un}\nFullName: {self.inputs['Full Name'].get()}\nEmail: {self.inputs['Email'].get()}\nType: standard\n"
        o, e = P4Wrapper.RunCommand("user -f -i", spec)
        
        if e: 
            self.logger.Log(f"User Create Error: {e}", True)
            # If user creation failed, we probably shouldn't continue
        else:
            # NEW: Log success output so we know it happened
            self.logger.Log(f"User Created: {o.strip()}") 

        # 2. Set Password
        pw = self.inputs["Password"].get()
        # o, e = P4Wrapper.RunCommand(f"passwd -u {un}", f"{pw}\n{pw}\n")
        o, e = P4Wrapper.SetPassword(un, pw)
        if e: self.logger.Log(f"Pwd Error: {e}", True)

        # 3. Add to Group (Using new helper)
        success, msg = P4Wrapper.AddUserToGroup(un, grp)
        if not success:
            self.logger.Log(msg, True)
        else:
            self.logger.Log(msg)
        
        # ... (Log saving logic) ...
        self.created_log.append([un, self.inputs["Full Name"].get(), self.inputs["Email"].get(), pw, grp, "Created"])
        self.idx += 1; self.ShowCurrent()
    
    def LockUI(self):
        for e in self.inputs.values(): e.configure(state="disabled")
    def UnlockUI(self):
        for e in self.inputs.values(): e.configure(state="normal")
        self.inputs["Username"].focus()
    
    def LoadCSV(self):
        p = os.path.join(os.getcwd(), "perforce_users.csv")
        if not os.path.exists(p):
            self.logger.Log("CSV not found. Import Excel first.", True)
            return
        try:
            with open(p, 'r') as f:
                self.users = list(csv.DictReader(f))
            self.created_log = [] 
            self.logger.Log(f"Loaded {len(self.users)} users.")
            self.idx = 0
            self.ShowCurrent()
        except Exception as e: self.logger.Log(f"CSV Error: {e}", True)
    
    def ShowCurrent(self):
        if self.idx >= len(self.users):
            self.logger.Log("All users processed.")
            self.SaveLog() 
            return
        u = self.users[self.idx]
        gen_u = (u['FirstName']+u['LastName']).replace(" ","")
        gen_p = u.get('Password')
        if not gen_p: gen_p = "".join(random.choices("ABCdef123!@#", k=12))
        self.UnlockUI()
        for k, v in [("Username", gen_u), ("Full Name", f"{u['FirstName']} {u['LastName']}"), ("Email", u['Email']), ("Password", gen_p)]:
            self.inputs[k].delete(0, "end"); self.inputs[k].insert(0, v)
        self.LockUI()
    
    def SaveLog(self):
        if not self.created_log: return
        path = os.path.join(os.getcwd(), "created_users_log.csv")
        try:
            with open(path, 'w', newline='') as f:
                writer = csv.writer(f)
                writer.writerow(["Username", "FullName", "Email", "Password", "Group"])
                writer.writerows(self.created_log)
            self.logger.Log(f"Saved creation log to: {path}")
            # Try to open file
            if os.name == 'nt': os.startfile(path)
            elif sys.platform == 'darwin': subprocess.call(['open', path])
            else: subprocess.call(['xdg-open', path])
        except Exception as e:
            self.logger.Log(f"Failed to save log: {e}", True)

    def Skip(self):
        self.logger.Log(f"Skipped {self.inputs['Username'].get()}")
        self.idx += 1; self.ShowCurrent()
    

class DeleterScreen(BaseScreen):
    def SetupUI(self):
        ctk.CTkButton(self, text="← Back", width=80, command=lambda: self.controller.show_screen("MainMenu")).pack(anchor="w", padx=20, pady=20)
        ctk.CTkLabel(self, text="User Deleter", font=("Arial", 24, "bold")).pack(pady=5)
        self.entry_group = ctk.CTkEntry(self, placeholder_text="Group Name", width=300)
        self.entry_group.pack(pady=10)
        ctk.CTkButton(self, text="Fetch Users", command=self.Fetch).pack()
        self.info_lbl = ctk.CTkLabel(self, text="Waiting...", font=("Arial", 16))
        self.info_lbl.pack(pady=20)
        frame = ctk.CTkFrame(self, fg_color="transparent")
        frame.pack()
        ctk.CTkButton(frame, text="Skip", command=self.Skip, fg_color="gray").pack(side="left", padx=10)
        ctk.CTkButton(frame, text="DELETE", command=self.Delete, fg_color="red").pack(side="left", padx=10)
        self.logger = self.controller.create_logger(self)
        self.logger.pack(fill="both", expand=True, padx=20, pady=20)
        
        self.users = []
        self.processed_log = [] 
        self.idx = -1
    
    def Fetch(self):
        g = self.entry_group.get()
        if not g: return
        self.logger.Log(f"Fetching group spec '{g}'...")
        
        out, err = P4Wrapper.RunCommand(f"group -o {g}")
        if err and "group not found" not in err.lower(): 
            self.logger.Log(err, True); return
        
        usernames = []
        in_users_section = False
        
        for line in out.splitlines():
            if line.startswith("Users:"):
                in_users_section = True
                continue
            if in_users_section:
                if not line.startswith("\t") and not line.startswith(" "):
                    in_users_section = False 
                    continue
                if line.strip(): usernames.append(line.strip())

        if not usernames:
            self.logger.Log("No users found.", True); return

        self.logger.Log(f"Found {len(usernames)} members. Fetching details...")
        user_list_str = " ".join(usernames)
        out, err = P4Wrapper.RunCommand(f"users {user_list_str}")
        
        if err: self.logger.Log(err, True); return

        self.users = []
        self.processed_log = [] 
        for line in out.splitlines():
            try:
                parts = line.split()
                if not parts: continue
                u_name = parts[0]
                
                email = "Unknown"
                if "<" in line and ">" in line:
                    email = line[line.find("<")+1 : line.find(">")]
                
                name = "Unknown"
                if "(" in line and ")" in line:
                    name = line[line.find("(")+1 : line.find(")")]
                
                self.users.append({'u': u_name, 'e': email, 'n': name})
            except: pass
        
        self.logger.Log(f"Loaded {len(self.users)} user details.")
        self.idx = 0; self.ShowCurrent()
    
    def ShowCurrent(self):
        if self.idx >= len(self.users): 
            self.info_lbl.configure(text="Done.")
            self.SaveAndOpenLog()
            return
        
        u = self.users[self.idx]
        self.info_lbl.configure(text=f"Current user: {u['u']}\n({u['n']} | {u['e']})")
    
    def Skip(self):
        u = self.users[self.idx]
        
        self.logger.Log(f"Skipped: {u['u']}")
        P4Wrapper.LogMockEvent(f"Skipped user: {u['u']}")
        self.processed_log.append({'user': u, 'action': 'Skipped'})
        
        self.idx += 1; self.ShowCurrent()
    
    def Delete(self):
        u = self.users[self.idx]
        
        P4Wrapper.LogMockEvent(f"Deleting user {u['u']}...")
        self.logger.Log(f"Deleting {u['u']}...", True)

        o, e = P4Wrapper.RunCommand(f"user -d -f {u['u']}")
        if e: self.logger.Log(e, True)
        else: self.logger.Log("Deleted.")
        
        self.processed_log.append({'user': u, 'action': 'Deleted'})

        self.idx += 1; self.ShowCurrent()

    def SaveAndOpenLog(self):
        if not self.processed_log: return
        path = os.path.join(os.getcwd(), "deleted_users_log.csv")
        try:
            with open(path, 'w', newline='') as f:
                writer = csv.writer(f)
                writer.writerow(["Username", "FullName", "Email", "Action"])
                for item in self.processed_log:
                    u = item['user']
                    writer.writerow([u['u'], u['n'], u['e'], item['action']])
            
            self.logger.Log(f"Saved log to: {path}")
            
            # Open file
            if os.name == 'nt': os.startfile(path)
            elif sys.platform == 'darwin': subprocess.call(['open', path])
            else: subprocess.call(['xdg-open', path])
            
        except Exception as e:
            self.logger.Log(f"Failed to save log: {e}", True)