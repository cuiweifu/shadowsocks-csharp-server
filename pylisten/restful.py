#!/usr/bin/env python
# -*- coding: utf-8 -*-

from twisted.web.server import Site
from twisted.web.resource import Resource

from twisted.internet import reactor

import logging
import Config
import restful_cmd
#logging.basicConfig(level=logging.DEBUG)

def run_web_server():
    root = Resource()
    root.putChild("cmd", restful_cmd.Cmd())

    factory = Site(root)
    reactor.listenTCP(Config.REST_LISTEN_PORT, factory)
    reactor.run()
