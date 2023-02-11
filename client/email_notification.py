import smtplib
from email.message import EmailMessage

# Try to log in to server and send email

EMAIL_CONFIG = {
    'host': 'mail.example.com',
    'port': 465,
    'user': 'mail-user',
    'password': 'mail-pass',
    'from': 'mail-user@example.com',
    'to': 'mail-user@example.com',

    'subject': 'DDNS {domain} automatic notification',
    'content': 'IP address updated.\nDomain: {domain}\nIP: {ip}\n',
}

def notify_email(domain: str, ip: str):
    c = EMAIL_CONFIG

    msg = EmailMessage()
    msg['From'] = c['from']
    msg['To'] = c['to']
    msg['Subject'] = str(c['subject']).replace('{ip}', ip).replace('{domain}', domain)
    msg.set_content(str(c['content']).replace('{ip}', ip).replace('{domain}', domain))

    with smtplib.SMTP_SSL(c['host'], c['port']) as smtp:
        try:
            smtp.login(c['user'], c['password'])
            smtp.send_message(msg)
        finally:
            smtp.quit()

if __name__ == "__main__":
    notify_email('example.com', '0.0.0.0')
