use std::{collections::HashMap};

use wmi::*;


pub fn get_cimos() -> Result<HashMap<String, Variant>, Box<dyn std::error::Error>> {
    let com_con = wmi::COMLibrary::new()?;
    let wmi_con = WMIConnection::new(com_con.into())?;

    let results: Vec<HashMap<String, Variant>> = wmi_con.raw_query("SELECT * FROM Win32_OperatingSystem")?;
    
    let mut this_one: HashMap<String, Variant> = HashMap::new();
    // We know that there will be only one at most
    for os in results {
        this_one = os;
    }


    Ok(this_one)
}
