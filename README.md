# mgrokutil


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


