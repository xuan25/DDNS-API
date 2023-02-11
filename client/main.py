import time
from ddns_api import update_record
from email_notification import notify_email

from log import append_info
from ns import fetch_ip, resolve_host

HOSTNAME = 'a.example.com'

if __name__ == '__main__':
    while True:
        append_info(f"Fetching IP...")
        current_ip = fetch_ip()
        append_info(f"    {current_ip}")

        append_info(f"Resolving IP...")
        resolved_ip = resolve_host(HOSTNAME)
        append_info(f"    {resolved_ip}")

        if current_ip and current_ip != resolved_ip:
            append_info("Updating DNS record...")
            update_record(HOSTNAME, 'A', 1, current_ip)

            append_info("Posting notification...")
            notify_email(HOSTNAME, current_ip)

            append_info("Updating complete.")
        else:
            append_info("Identical. Skip.")

        time.sleep(30*10)
