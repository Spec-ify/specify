# Overview of Specify

## Web
### Configuring the client application (now obselete)
The downloaded executable will follow the name format of: `specify-XX.exe`, where XX is a hex byte. This allows setting different config options by renaming the file


## Client
The client side is written in C#, and targets .net version 4.6. It collects the system info and writes it to `specify_specs.json`.

### Now obselete, ignore
The application checks its name, and uses the hex byte past `specify-` as a config. 
| Bit | Config Option                 |
|-----|-------------------------------|
| 0000000**1** | Run as Administrator | 

Once the config is loaded, the application will gather the needed information, and create a json. It will then either *put a .json file in the same directory as the executable*, or *send a POST request to the server, and open the response URL in the default browser*.
