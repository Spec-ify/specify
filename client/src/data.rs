/**
* Functions that get data from the system in a managable format
*/

use std::{
    array,
    collections::{hash_map, HashMap},
    vec, hash::Hash, io::Error, fmt::Pointer,
};
use windows::Win32::{
    Foundation::BSTR,
    System::{
        Com::{self, COINIT_MULTITHREADED, VARIANT},
        TaskScheduler::{self, IEnumWorkItems, ITaskService, TaskScheduler, TASK_ENUM_HIDDEN},
    },
};
use winreg::{enums::HKEY_LOCAL_MACHINE, RegKey};
use wmi::*;

type WMIMap = HashMap<String, Variant>;
type WMIVec = Vec<WMIMap>;
type DumbResult<T> = Result<T, Box<dyn std::error::Error>>;

//We use a thread local here to make sure every access happens on the same thread, preventing memory corruption
thread_local!(static COM_CON: COMLibrary = wmi::COMLibrary::new().unwrap());

/**
 * Makes a WMI connection, using the default namespace (probably ROOT).
 *This exists to prevent typing it out all the time
 */
fn get_wmi_con() -> DumbResult<WMIConnection> {
    COM_CON.with(|com_con| {
        let wmi_con = WMIConnection::new(*com_con)?;
        return Ok(wmi_con);
    })
}

/**
 * Makes a WMI connection, using a specified namespace.
 * This exists to prevent typing it out all the time.
 */
fn get_wmi_con_namespace(namespace: &str) -> DumbResult<WMIConnection> {
    // wrapping wmi connections in a local thread
    COM_CON.with(|com_con| {
        let wmi_con = WMIConnection::with_namespace_path(namespace, *com_con)?;
        return Ok(wmi_con);
    })
}

pub fn get_cimos() -> DumbResult<WMIMap> {
    let wmi_con = get_wmi_con()?;
    let results: WMIVec = wmi_con.raw_query("SELECT * FROM Win32_OperatingSystem")?;
    let mut this_one: WMIMap = HashMap::new();

    // We know that there will be only one at most
    for os in results {
        this_one = os;
    }

    Ok(this_one)
}

/**
 * Get the AV/Firewall information, in a tuple. AV is first, firewall is second.
 * If Windows Defender is used as a firewall, it will be blank. This is because of the WMI value.
 */
pub fn get_avfw() -> DumbResult<(WMIVec, WMIVec)> {
    let wmi_con = get_wmi_con_namespace(r"ROOT\SECURITYCENTER2")?;
    let av_results: Vec<WMIMap> = wmi_con.raw_query("SELECT * FROM AntivirusProduct")?;
    let fw_results: Vec<WMIMap> = wmi_con.raw_query("SELECT * FROM FirewallProduct")?;

    Ok((av_results, fw_results))
}

/**
* Obtaining cpu information as a hashmap
*/
pub fn get_cpu() -> DumbResult<HashMap<String, Variant>> {
    let wmi_con = get_wmi_con_namespace(r"Root\CIMV2")?;
    let result: Vec<HashMap<String, Variant>> =
        wmi_con.raw_query("SELECT * FROM Win32_Processor")?;
    let mut cpu_info: HashMap<String, Variant> = HashMap::new();
    for i in result {
        cpu_info = i;
    }

    Ok(cpu_info)
}

pub fn get_key() -> DumbResult<(String, String, String, String, String)> {
    let hklm = RegKey::predef(HKEY_LOCAL_MACHINE);
    let cver: RegKey = hklm.open_subkey(r"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?;

    let mut product_id: Vec<u16> = cver.get_raw_value("DigitalProductId")?.bytes.iter().map(|e| e.clone() as u16).collect();

    let mut key_output = String::new();
    let key_offset = 52;

    let is_win10 = (product_id[66] / 6) & 1;
    product_id[66] = (product_id[66] & 0xF7) | ((is_win10 & 2) * 4);
    let mut i = 24; let mut last: u16;
    let maps: Vec<char> = Vec::from("BCDFGHJKMPQRTVWXY2346789").iter().map(|e| e.clone() as char).collect();
    while {
        let mut current = 0;
        let mut j: i16 = 14;
        while {
            current = current * 256;
            current = product_id[(j + key_offset) as usize] + current;
            product_id[(j + key_offset) as usize] = current / 24;
            current = current % 24;
            j -= 1;

            j >= 0
        } {}

        i -= 1;
        let slice = maps[current as usize];
        key_output = slice.to_string() + &key_output;
        last = current;

        i >= 0
    } {}

    if is_win10 == 1 {
        let keypart1 = &key_output[1 as usize..(last+1) as usize];
        let keypart2 = &key_output[(last + 1) as usize..(key_output.len()) as usize];
        key_output = keypart1.to_string() + "N" + keypart2;
    }

    Ok((
        String::from(&key_output[0..5]),
        String::from(&key_output[5..10]),
        String::from(&key_output[10..15]),
        String::from(&key_output[15..20]), 
        String::from(&key_output[20..25])
    ))
}

pub fn get_audio() -> DumbResult<Vec<HashMap<String, String>>> {
    let wmi_con = get_wmi_con()?;
    let results: Vec<HashMap<String, Variant>> = wmi_con.raw_query("SELECT name, productname FROM win32_sounddevice")?;
    let mut output: Vec<HashMap<String, String>> = Vec::new();

    for result in results {
        let mut new_hash: HashMap<String, String> = HashMap::new();
        for (k, v) in result {
            let f = format!("{:?}", v);
            new_hash.insert(k, f[8..(f.len()-2)].to_string());
        }
        output.push(new_hash);
    }

    Ok(output)
}

pub fn get_cim_startups() -> DumbResult<Vec<String>> {
    let wmi_con = get_wmi_con()?;
    let results: Vec<HashMap<String, String>> = wmi_con.raw_query("SELECT Caption FROM Win32_StartupCommand")?;
    let caption_list: Vec<String> = results.iter().map(|e| e.get("Caption").unwrap().clone()).collect();

    Ok(caption_list)
}

/**
 * Inspired from https://github.com/j-hc/windows-taskscheduler-api-rust
 */
pub fn get_ts_startups() -> DumbResult<()> {
    unsafe {
        Com::CoInitializeEx(std::ptr::null_mut(), COINIT_MULTITHREADED)?;

        let ts: ITaskService = Com::CoCreateInstance(&TaskScheduler, None, Com::CLSCTX_ALL)?;
        ts.Connect(None, None, None, None)?;

        let root_folder = ts.GetFolder(&BSTR::from(r"\"))?;
        let tasks = root_folder.GetTasks(0)?;

        //println!("{:#?}", tasks.get_Item(0 as usize)?);

        Com::CoFreeAllLibraries();
        Ok(())
    }
}
