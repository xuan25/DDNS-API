import email.message
import email.utils
import smtplib

from config import EMAIL_CONFIG

# Try to log in to server and send email

def notify_email(domain: str, ip: str):
    c = EMAIL_CONFIG

    msg = email.message.EmailMessage()
    msg['From'] = c.mail_from
    msg['To'] = c.mail_to
    msg['Subject'] = str(c.subject).replace('{ip}', ip).replace('{domain}', domain)
    msg['Date'] = email.utils.localtime()
    msg['Message-ID'] = email.utils.make_msgid()
    msg.set_content(str(c.content).replace('{ip}', ip).replace('{domain}', domain))

    with smtplib.SMTP_SSL(c.host, c.port) as smtp:
        try:
            smtp.login(c.user, c.passwd)
            smtp.send_message(msg)
        finally:
            smtp.quit()

if __name__ == "__main__":
    notify_email('example.com', '0.0.0.0')
