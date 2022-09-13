use std::io;

// Credit to https://stackoverflow.com/a/65393488/
#[cfg(windows)]
fn main() -> io::Result<()> {
    use winres::WindowsResource;

    WindowsResource::new()
        // This path can be absolute, or relative to your crate root.
        .set_icon("assets/SNOO_256.ico")
        .compile()?;
    Ok(())
}

#[cfg(not(windows))]
fn main() {
    panic!("You must target Windows!");
}
