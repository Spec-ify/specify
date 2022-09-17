/*
* implement functions from other files
*/

#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")] // hide console window on Windows in release

pub mod config;
pub mod data;
pub mod gui;
pub mod monolith;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    //println!("{:#?}", data::get_cimos()?);
    //println!("{}", data::get_bootmode()?);
    //println!("{:#?}", monolith::MonolithBasicInfo::create()?);
    // println!("{:#?}", data::get_avfw()?);
    // println!("{:#?}", data::get_cpu()?);
    //println!("{:#?}", config::BAD_PROCESSES);
    // println!("{:#?}", data::get_key()?);
    // println!("{:#?}", data::get_cim_startups()?);
    // println!("{:#?}", data::get_audio()?);
    // println!("{:#?}", data::get_licenses()?);
    // println!("{:#?}", data::get_services()?);
    // data::get_ts_startups()?;
    // println!("{:#?}", data::get_ram()?);

    gui::run()?;

    Ok(())
}
