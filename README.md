Can the app with argument --help to see:

```
MSP-Challenge Client Launcher
-------------------------------
This program can launch multiple MSP-Challenge clients keeping the system memory usage under a certain limit.
It will pass any unknown arguments to the MSP-Challenge client, so you can pass client command line arguments, e.g.:
  MSPChallenge_Client_Launcher --num-clients=20 Team=Admin User=marin Password=test ServerAddress=http://localhost ConfigFileName=North_Sea_basic AutoLogin=1
-------------------------------
MSPChallenge-Client-Launcher 1.0.0+b6fd5a1897f71429f557d38d1ac82fa226a34cf0
Copyright (C) 2025 MSPChallenge-Client-Launcher

  --msp-client-folder-path                  Specifies the path to the MSP-Challenge client folder. It should contain the MSP-Challenge.exe.

  --num-clients                             (Default: 1) Specifies the number of clients to launch. Default is 1. **Number is limited by the system memory.** see --memory-limit-percentage and --memory-penalty-per-client-percentage.

  --client-group-size                       (Default: 5) Specifies the number of clients in each group. Default is 5.

  --delay-between-clients-sec               (Default: 4) Specifies the delay between clients in seconds. Default is 4.

  --delay-between-client-groups-sec         (Default: 20) Specifies the delay between client groups in seconds. Default is 20.

  --memory-limit-percentage                 (Default: 80) Specifies the memory limit as a percentage of total system memory. Default is 80%.

  --memory-penalty-per-client-percentage    (Default: 0,4) Specifies the memory penalty per client as a percentage of total system memory. Default is 0.4%.

  --kill-all-client-processes-at-start      (Default: false) Specifies whether to kill all the client processes at the start. Default is false.

  --help                                    Display this help screen.

  --version                                 Display version information.
```
