# MyCobot Pro 450: Starting myStudio Pro

This guide explains how to start the MyCobot Pro 450 control server and web backend, then open myStudio Pro from a website.

## 1. Connect the robot to your PC
[MyCobot Pro 450 myStudio Pro First Use Guide](https://docs.elephantrobotics.com/docs/mycobot-pro450-en/3-FunctionsAndApplications/5-BasicApplication/5.3-myStudioPro/5.3.1-mystudiofirstuse.html) use this link to connect the robot to your PC. The steps are summarized below.

Connect the Ethernet cable between the MyCobot dock and your PC.

Configure the PC Ethernet adapter with:

- **IP address:** `192.168.0.1`
- **Subnet mask:** `255.255.255.0`

The robot's IP address is `192.168.0.232`.

## 2. SSH into the robot

Open Command Prompt on Windows and connect to the robot:
```bash
ssh root@192.168.0.232
```

If connection fails, check that the robot is addressable by pinging it in cmd:
```bash
ping 192.168.0.232
```


When prompted, enter the password:
```text
root
```

If the connection succeeds, you should see a prompt similar to:
```text
root@ok3562:~#
```

## 3. Start MyCobotPro

In the SSH terminal, go to the MyCobotPro folder and check its contents:
```bash
cd /root/MyCobot450/bin
ls
```

You should see:
```text
MyCobotPro
MyCobotPro-socket
```

Start the robot control server:
```bash
./MyCobotPro
```

Wait until you see:
```text
[INFO] 启动 Unix Socket 服务: /tmp/mycobotpro450.sock
```

This confirms that MyCobotPro has started successfully. Leave this terminal running.

## 4. Open a second SSH terminal
Open another Command Prompt window and connect to the robot again:
```bash
ssh root@192.168.0.232
```

Enter the password when prompted:
```text
root
```

## 5. Start the web backend
In the second SSH terminal, run:
```bash
/opt/webapp/api/launcher.sh
```

Wait for the startup messages, then leave this terminal running as well.

At this point, both services should be running:

| Terminal | Command | Service |
| --- | --- | --- |
| 1 | `./MyCobotPro` | Robot control server |
| 2 | `/opt/webapp/api/launcher.sh` | Web/API service |

## 6. Open myStudio Pro

On your Windows PC, open Chrome or Edge and go to:

<http://192.168.0.232:8000>

If everything is running correctly, the myStudio Pro webpage should appear.

## Quick start

Use the following commands whenever you need to start the services manually.

### Terminal 1

```bash
ssh root@192.168.0.232
cd /root/MyCobot450/bin
./MyCobotPro
```

### Terminal 2

```bash
ssh root@192.168.0.232
/opt/webapp/api/launcher.sh
```

### Browser

Open <http://192.168.0.232:8000>.

> **Note:** If MyCobotPro is already running, open a second SSH terminal and run only `launcher.sh`.