shadowsocks
===============

A fast tunnel proxy that helps you bypass firewalls.

Install
-------

Makesure installed Runtime .Net 2.0 and Visual C++ 2010 Redistributable http://www.microsoft.com/en-us/download/details.aspx?id=5555

Install
-------

Edit config.json

    {
        "server":"my_server_ip",
        "server_port":8388,
        "password":"mypassword",
        "timeout":300,
        "method":"aes-128-cfb",
        "multiuser_pylisten":false
    }

`multiuser_pylisten` is set for control multi server by python

Multi server
-----------------

Multi server used to create multi server and control multi server.

install database https://github.com/mengskysama/shadowsocks/tree/manyuser

install python 2.7、twisted and cymysql(https://github.com/nakagami/CyMySQL)

run `shadowsocks-server.exe` and `server.py`

if you want a font end go to here https://github.com/mengskysama/MakeDieSS

Encrypt method
-----------------
 
    "rc4",
    "aes-256-cfb",
    "aes-192-cfb",
    "aes-128-cfb",
    "bf-cfb"

License
-----------------
MIT

Bugs and Issues
----------------
