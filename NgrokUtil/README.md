# mgrokutil
WPF Console app to generate an ngrok TCP tunnels config file. 

- Generate a ngrok.yml with the app.
- Noramlly leave the path as default so ngrok can easily find it.
- And normally leave the network 192.168.0.0 as local as default
- You will need your authtoken, generated as part of the setup.
- For ngrok setup see [Softata: ngrok update](https://davidjones.sportronics.com.au/softata/Softata-ngrok_update-softata.html)
  - An update page coming specifically for this
  

```
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
<img width="1578" height="946" alt="image" src="https://github.com/user-attachments/assets/77766ec3-2678-4dd7-bf62-37cacf2bd35c" />

Once generated run with 

```
ngrok start --all
```
Or you can just name the tunnels eg ```ngrot start tcp11``` etc

<img width="2350" height="818" alt="image" src="https://github.com/user-attachments/assets/47081840-8fde-4a6f-b584-f0f00b50d3d5" />



