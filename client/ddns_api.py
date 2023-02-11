import requests

ENDPOINT = 'https://api.example.com/ddns/update'
USER = 'user'
PASS = 'pass'

def update_record(host, type, ttl, data):
    payload = {
        'host': host,
        'type': type,
        'ttl': ttl,
        'data': data,
    }
    with requests.post(ENDPOINT, json=payload, auth=(USER, PASS)) as r:
        return r.ok, r.status_code, r.text

if __name__ == "__main__":
    ok, status_code, text = update_record('a.example.com', 'A', 1, '0.0.0.0')
    print(ok)
    print(status_code)
    print(text)
