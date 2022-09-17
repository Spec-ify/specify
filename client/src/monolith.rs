/*
 * The json object of all the things
 */

use serde::{Serialize, Deserialize};
use wmi::COMLibrary;

use crate::data::{self, WMISystem};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Monolith {
    pub basic_info: MonolithBasicInfo
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct MonolithBasicInfo {
    pub caption: String,
    //pub install_date: String,
    //pub boot_time: String,
    //pub hostname: String,
    //pub domain: String,
    //pub boot_mode: String,
    //pub boot_state: String
}

impl MonolithBasicInfo {
    pub fn create() -> data::DumbResult<Self> {
        let os = data::get_cimos()?;
        //let system = WMISystem::default();
        //let bootmode = data::get_bootmode()?;

        Ok(Self {
            caption: os.caption,
            //install_date: format!("{:?}", os.install_date),
            //boot_time: format!("{:?}", os.last_boot_up_time),
            //hostname: system.caption,
            //domain: system.domain,
            //boot_mode: bootmode,
            //boot_state: system.bootup_state
        })
    }
}

impl Monolith {
    pub fn jsonify(&self) -> serde_json::Result<String> {
        serde_json::to_string_pretty(&self)
    }
}

pub fn assemble_monolith() {
    println!("🗿");

    let basic_info = match MonolithBasicInfo::create() {
        Ok(r) => r,
        Err(_e) => panic!("error getting basic info")
    };

    let m: Monolith = Monolith {
        basic_info
    };

    let j: String = match m.jsonify() {
        Ok(r) => r,
        Err(_e) => panic!("failed converting to json")
    };

    println!("{}", j);
}
