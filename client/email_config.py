class EmailConfig():
    def __init__(self, host, port, user, passwd, mail_from, mail_to, subject, content) -> None:
        self.host = host
        self.port = port
        self.user = user
        self.passwd = passwd
        self.mail_from = mail_from
        self.mail_to = mail_to
        self.subject = subject
        self.content = content
