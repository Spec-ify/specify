/**
Hardcoded configs (badlists, etc)
*/
use winreg::{enums::HKEY_LOCAL_MACHINE, HKEY};

pub static BAD_SOFTWARE: &[&str] = &[
    "Driver Booster*",
    "iTop*",
    "Driver Easy*",
    "Roblox*",
    "ccleaner*",
    "Malwarebytes*",
    "Wallpaper Engine*",
    "Voxal Voice Changer*",
    "Clownfish Voice Changer*",
    "Voicemod*",
    "Microsoft Office Enterprise 2007*",
    "Memory Cleaner*",
    "System Mechanic*",
    "MyCleanPC*",
    "DriverFix*",
    "Reimage Repair*",
    "cFosSpeed*",
    "Browser Assistant*",
    "KMS*",
    "Advanced SystemCare",
    "AVG*",
    "Avast*",
    "salad*",
    "McAfee*",
];

pub const BAD_STARTUP: &[&str] = &[
    "AutoKMS",
    "kmspico",
    "McAfee Remediation",
    "KMS_VL_ALL",
    "WallpaperEngine",
];

pub const BAD_PROCESSES: &[&str] = &[
    "MBAMService",
    "McAfee WebAdvisor",
    "Norton Security",
    "Wallpaper Engine Service",
    "Service_KMS.exe",
    "iTopVPN",
    "wallpaper32",
    "TaskbarX",
];

<<<<<<< HEAD
pub const BAD_KEYS: &[(HKEY, &str)] = &[(
    HKEY_LOCAL_MACHINE,
    r"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds\",
)];
=======
pub const BAD_KEYS: &[(HKEY, &str)] = &[
    (HKEY_LOCAL_MACHINE, r"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds\"),
    (HKEY_LOCAL_MACHINE, r"SYSTEM\Setup\LabConfig")
];
>>>>>>> dca83557bea4bc421ac6206f9df442efcb8cdde4
