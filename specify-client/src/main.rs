mod data;
mod config;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("{:#?}", data::get_cimos()?);
    println!("{:#?}", data::get_avfw()?);

    //println!("{:#?}", config::BAD_PROCESSES);

    Ok(())
}
