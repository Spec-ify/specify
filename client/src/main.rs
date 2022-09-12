/*
* implement functions from other files
*/

#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")] // hide console window on Windows in release

mod config;
mod data;
mod gui;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    // println!("{:#?}", data::get_cimos()?);
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
