# Ngrokutil and NgrokTunnelsConfig apps
- `Ngrokutil` A WPF Console app to generate an ngrok TCP tunnels config file. 
- And `NgrokTunnelsConfig` A WPF Form based  app to do same.
---
- Generate a ngrok.yml with the app.
- Noramlly leave the path as default so ngrok can easily find it.
- And normally leave the network 192.168.0.0 as local as default
- You will need your authtoken, generated as part of the setup.
- For ngrok setup see [Softata: ngrok update](https://davidjones.sportronics.com.au/softata/Softata-ngrok_update-softata.html)
  - An update page coming specifically for this
  

```

## Ngrokutil
---------------------------
MgrokUtil Help
---------------------------
MgrokUtil command line options:
  --help, -h
      Show this help and exit.
  --clear, -c
      Clear persisted settings (Path/AuthToken/Port) and exit.
  --path=<file>, -p=<file>  [Default]
      Default: %HOMEPATH%\AppData\Local\ngrok\ngrok.yml
      Path to ngrok.yml ('.yml' is appended if missing).
      If a single positional argument is provided, it is treated as the path.
  --authtoken=<token>, -a=<token>
      Optional ngrok authtoken (must be 49 chars, [A-Za-z0-9_]).
      If the config file is missing, providing a valid authtoken will create it.
  --port=<port>, -t=<port>
      Default: 4242.
      If existing tunnels contain an addr port, that port is used unless overridden by th e commandline.
  --network=<ipv4>, --nw=<ipv4>, -n=<ipv4>
      Network IPv4 used for tunnel addr generation; must exist locally.
      Default: 192.168.0.0
  --ipBase=<csv>, -i=<csv>
      CSV of ports/ids (1..234).
      If value starts with '+', merges with existing tcp<port> tunnels; otherwise replaces them.
---------------------------
```



## NgrokTunnelsConfig

- App is present, need to document.
- For now just make changes and save.



Once generated run with 

```
ngrok start --all
```
Or you can just name the tunnels eg ```ngrot start tcp11``` etc

<img width="800" height="463" alt="image" src="https://github.com/user-attachments/assets/a4994ad5-3fcb-43b6-ac0d-2d58dab26376" />


### Update:
- Can new orchestrate `ngrok start --all` from `Tunnels` menu:
  - **Start** _Runs ngrok start --all_
    - _Then copy the Shell text._
  - **Capture** _Get list of tunnels from text on Clipboard._
  - **Select** _From list._
- Get list of tunnels to choose from such as
```
tcp://0.tcp.au.ngrok.io:13964 -> 192.168.0.5:4242
tcp://0.tcp.au.ngrok.io:18348 -> 192.168.0.4:4242
```
Can then select and get eg `tcp://0.tcp.au.ngrok.io:13964' on Clipboard.
- Run `Tunnels.Stop ngrok.exe` before app exit.



