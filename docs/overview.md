# Overview of Specify

## Web
### Configuring the client application
The downloaded executable will follow the name format of: `specify-XX.exe`, where XX is a hex byte. This allows setting different config options by renaming the file


## Client
The application checks its name, and uses the hex byte past `specify-` as a config. 
| Bit | Config Option                 |
|-----|-------------------------------|
| 0000000**1** | Run as Administrator | 
