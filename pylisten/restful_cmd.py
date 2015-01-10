#!/usr/bin/env python
# -*- coding: utf-8 -*-

from twisted.web.resource import Resource
import sys
import logging
import Config
from server_pool import ServerPool
import json

class Cmd(Resource):
    isLeaf = True

    def render_GET(self, request):
        retcode = 1
        retmsg = 'unknow err'
        if request.uri.startswith('/cmd/del_server'):
            while True:
                try:
                    if not 'key' in request.args or Config.REST_APIKEY != request.args['key'][0]:
                        retmsg = 'key error'
                        break
                    port = int(request.args['port'][0])
                    if ServerPool.get_instance().del_server(port) is True:
                        retcode = 0
                        retmsg = 'success'
                except Exception, e:
                    retmsg = str(e)
                finally:
                    break

        elif request.uri.startswith('/cmd/new_server'):
            while True:
                try:
                    if not 'key' in request.args or Config.REST_APIKEY != request.args['key'][0]:
                        retmsg = 'key error'
                        break
                    port = int(request.args['port'][0])
                    passwd = request.args['passwd'][0]
                    ret =  ServerPool.get_instance().new_server(port, passwd)
                    if ret is True:
                        retcode = 0
                        retmsg = 'success'
                    else:
                        retmsg = ret
                except Exception, e:
                    retmsg = str(e)
                finally:
                    break
                
        return json.dumps({'code': retcode, 'msg': retmsg})
