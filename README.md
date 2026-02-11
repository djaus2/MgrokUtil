# Ngrokutil and NgrokTunnelsConfig apps
- `Ngrokutil` A WPF Console app to generate an ngrok TCP tunnels config file. 
- And `NgrokTunnelsConfig` A WPF Form based  app to do same and orchestrate `ngrok start -all`
---
- Generate a ngrok.yml with the app.
- Noramlly leave the path as default so ngrok can easily find it.
- And normally leave the network 192.168.0.0 as local as default
- You will need your authtoken, generated as part of the ngrok setup.
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

- App documentation below.


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
Can then select and get eg `tcp://0.tcp.au.ngrok.io:13964` on Clipboard.
- Run `File->Exit` to exit
  - Close ngrok procvess and window.
---
 
# NgrokTunnelsConfig — Menu Reference

> Menu items and behavior.

**Note:** The ngrok configuration is typically stored in `c:\users\CurrentUser\AppData\Local\ngrok\ngrok.yml`  
When you run, say, `ngrok start -all`, it uses the tunnels specified in that `ngrok.yml` file.

## File
- **Load**  
  - Action: Loads YAML from path bound to `Path`.  
  - Handler: `MenuFileLoad_Click`  
  - Effects: updates `RawYaml`, `RootNodes`, and `AuthToken`.

- **Save**  
  - Action: Writes current YAML to `Path` (creates directory if needed).  
  - Handler: `MenuFileSave_Click`

<br/>

- **Exit**  
  - Action: Attempts to stop ngrok and exits the application.  
  - Handler: `MenuFileExit_Click`  
  - Notes: Calls `MenuTunnelsStopNgrok_Click` and `MenuTunnelsStopNgrokExe_Click` before shutdown.

---

## Tunnels
- **Start**  
  - Action: Stops other `ngrok` instances and launches `ngrok start --all` in a shell.  
  - Handler: `MenuTunnelsStart_Click`  
  - Notes: Keeps process tracked in `_ngrokProcess`. UI shows instructions to copy shell output for capture.

- **Capture**  
  - Action: Parses the current clipboard text for lines beginning with `Forwarding`, extracts the `tcp://...` tail, deduplicates and stores them. Copies CSV to clipboard.  
  - Handler: `MenuTunnelsCapture_Click`  
  - Effects: Populates in-memory `Tunnels` list and copies CSV of captures to the clipboard.

- **Select**  
  - Action: Presents a dialog showing `Tunnels`. Double-click, Enter, or OK copies the selected tunnel token (substring up to the first space) to the clipboard.  
  - Handler: `MenuTunnelSelect_Click`

- **Stop**  
  - Action: Attempts to stop the process started by the app (`_ngrokProcess`) — polite close then kill fallback.  
  - Handler: `MenuTunnelsStopNgrok_Click`

- **Stop ngrok.exe**  
  - Action: Enumerates all `ngrok` processes (`Process.GetProcessesByName("ngrok")`) and tries to stop/kill them.  
  - Handler: `MenuTunnelsStopNgrokExe_Click`

<br/>

- **Add New / Replace / Remove / Clear**  
  - Actions: Modify tunnels in the YAML preview according to `IpBase` and `Network`.  
  - Handlers:  
    - **Add New** `MenuTunnelsAdd_Click` Adds tunnels to in-memory Tunnels Yaml  
    - **Replace** `MenuTunnelsReplace_Click` Replace tunnels in in-memory Tunnels  yaml  
    - **Remove** `MenuTunnelsRemove_Click` Removes tunnels in-memory Tunnels yaml  
    - **Clear** `MenuTunnelsClear_Click` Clears in-memory Tunnels yaml
    - ***Note:*** Need to run File->Save to lock in these changes to tunnels

---

## Settings
- **Save**  
  - Action: Persist app settings (`Path`, `AuthToken`, `Port`) via `AppSettingsStore`.  
  - Handler: `MenuSettingsSave_Click`

- **Clear**  
  - Action: Delete stored app settings.  
  - Handler: `MenuSettingsClear_Click`

---

## Help
- **Help**  
  - Action: Show help text.  
  - Handler: `MenuHelp_Click`

---

## Usage Notes
- Typical workflow:
  1. `Tunnels -> Start` to run `ngrok start --all`.
  2. Copy the shell output (leave ngrok running).
  3. `Tunnels -> Capture` to parse clipboard and populate `Tunnels`.
  4. `Tunnels -> Select` to pick a tunnel (double-click or press Enter) and copy its `tcp://...` token to the clipboard.

- Implementation details:
  - Captured lines are stored in the `Tunnels` `List<string>` as the full tail; selection extracts the token up to the first space.
  - Status and errors are surfaced via the view-model `Error` property (bound in the UI).





