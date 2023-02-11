
import socket

import requests
from log import append_warn

IP_CHECK_ENDPOINT = 'https://api.example.com/ip'

def fetch_ip():
    try:
        with requests.get(IP_CHECK_ENDPOINT) as r:
            ip_addr = r.text.strip()
        
        return ip_addr

    except Exception as ex:
        append_warn('Unable to check the ip.\n{ex}'.format(ex=ex))
        return None

def resolve_host(hostname):
    try:
        return socket.gethostbyname(hostname)
    except Exception as ex:
        append_warn('Cannot resolve hostname.\n{ex}'.format(ex=ex))
        return None