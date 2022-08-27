use std::io;

mod data;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("{:#?}", data::get_cimos()?);

    Ok(())
}
