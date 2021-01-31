# MultiLotrConquestServerLauncher

.NET C# console application used to launch and run multiple servers on different ports as background processes. Requirement is to apply the `Port Patch` on the `ConquestServer.exe` before, otherwise servers will run on the same hard coded port.

The MultiLotrConquestServerLauncher also detects if a server is crashed and will automatically re-launch this individual server instance.

## Configuration

The configuration is done using the file `MultiDedicated.xml` which is placed in `C:\Users\[UserName]\Documents\The Lord of the Rings - Conquest (Server PC)\` next to the regular `Dedicated.xml` file.
The MultiLotrConquestServerLauncher will read it, process each `Dedicated` node, read the linked dedicated file, write it into the real `Dedicated.xml`, launch the server, wait 30 seconds and continue with next server.

![Screenshot of XML inside MultiDedicated.xml](https://i.imgur.com/2v06IGQ.png "MultiDedicated.xml")

### Global Options (`Config` node)

|Option|Explanation|
|------|-----------|
|TargetFileName|Name of the server config file name (should always be `Dedicated.xml`)|
|ServerFilePath|The absolute file path to the `ConquestServer.exe`|
|LogLevel|Log Level - Can be `debug`, `info`, `warn`, `error` or `fatal`. The log file `MultiDedicated.log` is written into the `C:\Users\[UserName]\Documents\The Lord of the Rings - Conquest (Server PC)\` directory|

### Server Specific Options (`Dedicated` nodes)

|Option|Explanation|
|------|-----------|
|FileName|Name of the file to read the server config from. The file is basically just a variant of the ´Dedicated.xml`|
|GameName|Server name (optional), overrides the value of the `GameName` node in the `Dedicated.xml` (`Config->Plasma->GameName`)|
|Port|Server port (optional), overrides the value of the `Port` node in the `Dedicated.xml` (`Config->Plasma->Port`)|
|RandomStartLevel|Determines if the servers level rotation will begin at a random position but keeps the map order in general. Allowed values: `true` or `false`|
|RandomLevelOrder|Determines if the servers level rotation should be completly random. Allowed values: `true` or `false`. Please note: when `RandomLevelOrder` is enabled, the `RandomStartLevel` option is ignored.|

# Scheduled Task

This application can be easily configured as Scheduled Task (e.g. at startup) using the [Windows Task Scheduler](https://en.wikipedia.org/wiki/Windows_Task_Scheduler) so the  LOTRC servers start automatically after the server restarted. No permanent user login is required, the servers can run as background tasks without any GUI.