import logging
from logging.handlers import RotatingFileHandler
import sys
import time

LOG_FILE = '/var/log/ddns.log'
# LOG_FILE = 'ddns.log'

handler = RotatingFileHandler(LOG_FILE, 'a', 2*1024*1024, 4)
formatter = logging.Formatter('[%(asctime)s] [%(levelname)s] %(message)s')
handler.setFormatter(formatter)

logger = logging.getLogger()
logger.setLevel(logging.DEBUG)
logger.addHandler(handler)

def append_info(message):
    localtime = time.asctime(time.localtime(time.time()))
    print('[{localtime}] {message}'.format(localtime=localtime, message=message))
    sys.stdout.flush()
    logger.info(message)

def append_warn(message):
    localtime = time.asctime(time.localtime(time.time()))
    print('[{localtime}] {message}'.format(localtime=localtime, message=message))
    sys.stdout.flush()
    logger.warning(message)
