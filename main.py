import customtkinter as ctk
from gui.screens import LoginScreen, MainMenuScreen, ImporterScreen, CreatorScreen, DeleterScreen, SettingsScreen
from gui.logger import AppLogger

ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("dark-blue")

class P4ToolApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("Perforce Utils By Sai")
        self.geometry("500x650")
        self.resizable(True, True)

        self.container = ctk.CTkFrame(self)
        self.container.pack(fill="both", expand=True)
        self.container.grid_rowconfigure(0, weight=1)
        self.container.grid_columnconfigure(0, weight=1)

        self.frames = {}
        for F in (MainMenuScreen, LoginScreen, ImporterScreen, CreatorScreen, DeleterScreen, SettingsScreen):
            name = F.__name__.replace("Screen", "")
            frame = F(parent=self.container, controller=self)
            self.frames[name] = frame
            frame.grid(row=0, column=0, sticky="nsew")

        self.show_screen("MainMenu")

    def show_screen(self, name):
        frame = self.frames[name]
        frame.tkraise()
        if hasattr(frame, "RefreshUI"):
            frame.RefreshUI()

    def create_logger(self, parent):
        return AppLogger(parent, height=200)

if __name__ == "__main__":
    app = P4ToolApp()
    app.mainloop()