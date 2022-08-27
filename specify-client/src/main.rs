mod data;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("{:#?}", data::get_cimos()?);
    println!("{:#?}", data::get_avfw()?);

    Ok(())
}
