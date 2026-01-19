import customtkinter as ctk
import datetime

class AppLogger(ctk.CTkTextbox):
    def __init__(self, master, **kwargs):
        super().__init__(master, **kwargs)
        self.configure(state="disabled", font=("Consolas", 12))
        self.tag_config("error", foreground="#ff5555") 
        self.tag_config("info", foreground="#ffffff")
    
    def Log(self, message, is_error=False):
        timestamp = datetime.datetime.now().strftime("%H:%M:%S")
        full_msg = f"[{timestamp}] {message}\n"
        
        self.configure(state="normal")
        if is_error:
            self.insert("end", full_msg, "error")
        else:
            self.insert("end", full_msg, "info")
        self.configure(state="disabled")
        self.see("end")
        
    def GetText(self):
        self.configure(state="normal")
        text = self.get("1.0", "end-1c")
        self.configure(state="disabled")
        return text