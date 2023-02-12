# DDNS-API

A self-hosted Dynamic DNS solution based on *Bind 9*, including server and client implementations.

## Features

- HTTP API for DNS record update
- Automatic email notification
- Minimalism

## Requirements

- *Bind 9* (on server)
  - providing DNS query responds
  - including `nsupdate` for NS records updating
- *Caddy* (on server)
  - providing TLS and HTTP authorization
- *Python 3* (on client)
  - providing client runtime
- Email account with SMTP support
  - for Email notification

## Setup

### Files - Server

- `/usr/local/lib/ddns-api-server/DDNS-API-Server`
- `/etc/systemd/system/ddns-api-server.service`
- `/etc/caddy/Caddyfile`
- `/var/log/ddns-api-server/server.log`

### Files - Client

- `/usr/local/lib/ddns/*.py`
- `/etc/systemd/system/ddns.service`
- `/var/log/ddns.log`

### Configure Server

1. Configure hostname (`api.example.com`) for API endpoint in file `Caddyfile`
2. Configure authorization (`basicauth`) for API endpoint in file `Caddyfile`
3. Configure static DNS records and *Bind 9* as required (see [example](#example-setup-dns-and-bind-9))
4. Start services
   - `sudo systemctl start bind9`
   - `sudo systemctl start caddy`
   - `sudo systemctl start ddns-api-server`

### Configure Client

1. Configure API endpoint for DDNS in file `ddns_api.py`
   - `ENDPOINT`
   - `USER`
   - `PASS`
2. Configure API endpoint for IP checking in file `ns.py`
   - `IP_CHECK_ENDPOINT`
3. Configure email notification in file `email_notification.py`
   - `EMAIL_CONFIG`
4. Configure hostname of the client (domain name to update) in file `main.py`
   - `HOSTNAME`
5. Setup runtime environment in directory `/usr/local/lib/ddns/.env`
   - ```sh
     sudo bash
     cd /usr/local/lib/ddns
     python -m venv .env
     source .env/bin/activate
     pip install requests
     exit
     ```
6. Feel free to modify the script as required
7. Start the service
   - `sudo systemctl start ddns`

### Example: Setup DNS and *Bind 9*

- For example, we have:
  - a server host with static IP address: `x.y.z.w`
  - a client host with dynamic IP address
  - a domain name: `example.com`
- We want:
  - bind hostname `dyn-1.example.com` or `xxx.dyn-1.example.com` dynamically to the client host
- We need:
  - Add a few DNS records to the domain registrar in zone `example.com`
    | Host name | Type | TTL | Data | Comment |
    | --- | --- | --- | --- | --- |
    | api.example.com | A | 3600 | x.y.z.w | API server |
    | ns1.example.com | A | 3600 | x.y.z.w | Self-hosted NS server |
    | ns2.example.com | A | 3600 | x.y.z.w | Self-hosted NS server |
    | dyn-1.example.com | NS | 86400 | ns1.example.com. | Delegating sub domain to self-hosted NS server |
    | dyn-1.example.com | NS | 86400 | ns2.example.com. | Delegating sub domain to self-hosted NS server |
  - Edit options section in file `/etc/bind/named.conf.options`
    ```
    options {
    	allow-query { any; };
    	recursion no;

    	listen-on { any; };
    	listen-on-v6 { any; };
    };
    ```
  - Add a zone section for zone `dyn-1.example.com` in file `/etc/bind/named.conf.local`
    ```
    zone "dyn-1.example.com." {
    	type master;
    	file "/var/lib/bind/dyn-1.example.com.zone";
    	update-policy local;
    };
    ```
  - Create a zone file for zone `dyn-1.example.com` in file `/var/lib/bind/dyn-1.example.com.zone`
    ```
    $ORIGIN dyn-1.example.com.
    @       3600    IN      SOA     ns1.dyn-1.example.com. admin.dyn-1.example.com. (2023021101 14400 3600 604800 300)
    @       86400   IN      NS      ns1.dyn-1.example.com.
    @       86400   IN      NS      ns2.dyn-1.example.com.
    ns1     3600    IN      A       140.238.123.103
    ns2     3600    IN      A       140.238.123.103
    ```
  - Verify config and zone
    ```sh
    named-checkconf
    named-checkzone dyn-1.example.com /var/lib/bind/dyn-1.example.com.zone
    ```

## Example of Logs

### Client Logs

- Run `tail -f /var/log/ddns.log` on client

```
[2023-02-12 07:12:57,312] [INFO] Fetching IP...
[2023-02-12 07:12:57,318] [DEBUG] Starting new HTTPS connection (1): api.example.com:443
[2023-02-12 07:12:58,244] [DEBUG] https://api.example.com:443 "GET /ip HTTP/1.1" 200 14
[2023-02-12 07:12:58,251] [INFO]     a.b.c.d
[2023-02-12 07:12:58,253] [INFO] Resolving IP...
[2023-02-12 07:12:58,789] [WARNING] Cannot resolve hostname.
[Errno -5] No address associated with hostname
[2023-02-12 07:12:58,790] [INFO]     None
[2023-02-12 07:12:58,791] [INFO] Updating DNS record...
[2023-02-12 07:12:58,796] [DEBUG] Starting new HTTPS connection (1): api.example.com:443
[2023-02-12 07:13:04,267] [DEBUG] https://api.example.com:443 "POST /ddns/update HTTP/1.1" 200 148
[2023-02-12 07:13:04,274] [INFO] Posting notification...
[2023-02-12 07:13:07,119] [INFO] Updating complete.
[2023-02-12 07:18:07,220] [INFO] Fetching IP...
[2023-02-12 07:18:07,225] [DEBUG] Starting new HTTPS connection (1): api.example.com:443
[2023-02-12 07:18:07,898] [DEBUG] https://api.example.com:443 "GET /ip HTTP/1.1" 200 14
[2023-02-12 07:18:07,904] [INFO]     a.b.c.d
[2023-02-12 07:18:07,906] [INFO] Resolving IP...
[2023-02-12 07:18:08,225] [INFO]     a.b.c.d
[2023-02-12 07:18:08,226] [INFO] Identical. Skip.
```

```
[2023-02-12 07:23:08,327] [INFO] Fetching IP...
[2023-02-12 07:23:08,332] [DEBUG] Starting new HTTPS connection (1): api.example.com:443
[2023-02-12 07:23:09,040] [DEBUG] https://api.example.com:443 "GET /ip HTTP/1.1" 200 14
[2023-02-12 07:23:09,045] [INFO]     e.f.g.h
[2023-02-12 07:23:09,047] [INFO] Resolving IP...
[2023-02-12 07:23:09,358] [INFO]     a.b.c.d
[2023-02-12 07:23:09,359] [INFO] Updating DNS record...
[2023-02-12 07:23:09,363] [DEBUG] Starting new HTTPS connection (1): api.example.com:443
[2023-02-12 07:23:10,028] [DEBUG] https://api.example.com:443 "POST /ddns/update HTTP/1.1" 200 148
[2023-02-12 07:23:10,035] [INFO] Posting notification...
[2023-02-12 07:23:12,694] [INFO] Updating complete.
[2023-02-12 07:28:12,795] [INFO] Fetching IP...
[2023-02-12 07:28:12,800] [DEBUG] Starting new HTTPS connection (1): api.example.com:443
[2023-02-12 07:28:13,444] [DEBUG] https://api.example.com:443 "GET /ip HTTP/1.1" 200 14
[2023-02-12 07:28:13,451] [INFO]     e.f.g.h
[2023-02-12 07:28:13,453] [INFO] Resolving IP...
[2023-02-12 07:28:13,766] [INFO]     e.f.g.h
[2023-02-12 07:28:13,766] [INFO] Identical. Skip.
```

### Server Logs

- Run `tail -f /var/log/ddns-api-server/server.log` on server

```
[02/11/2023 23:13:03] [Info] [Request] a.b.c.d, 127.0.0.1:37988 (POST) http://api.example.com/
[02/11/2023 23:13:04] [Info] [Action] update delete dyn-1.example.com A
[02/11/2023 23:13:04] [Info] [Action] update add dyn-1.example.com 1 A a.b.c.d
[02/11/2023 23:13:04] [Info] [Action] send
[02/11/2023 23:13:04] [Info] [Action] quit
```

```
[02/11/2023 23:23:09] [Info] [Request] e.f.g.h, 127.0.0.1:60582 (POST) http://api.example.com/
[02/11/2023 23:23:09] [Info] [Action] update delete dyn-1.example.com A
[02/11/2023 23:23:09] [Info] [Action] update add dyn-1.example.com 1 A e.f.g.h
[02/11/2023 23:23:09] [Info] [Action] send
[02/11/2023 23:23:09] [Info] [Action] quit
```
