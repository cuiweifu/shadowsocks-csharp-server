#!/usr/bin/env python
# -*- coding: utf-8 -*-

# Copyright (c) 2014 clowwindy
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.
import os
import logging
import thread
import Config
from socket import *

class ServerPool(object):

    instance = None
    tcp_servers_pool = {}

    def __init__(self):
        #prot:[0:0]
        #假设不在list里的服务都没有启动
        self.tcp_servers_run = {}
        thread.start_new_thread(ServerPool.recv_transfer_thread, (Config.TRANSFER_PORT,))

    @staticmethod
    def recv_transfer_thread(port):
        s = socket(AF_INET, SOCK_DGRAM)
        s.bind(('127.0.0.1', port))
        while True:
            try:
                data, addr = s.recvfrom(512)
                arr_transfer = data.split(':')
                print arr_transfer
                if len(arr_transfer) == 3:
                    port = int(arr_transfer[0])
                    ul = long(arr_transfer[1])
                    dl = long(arr_transfer[2])
                    if port in ServerPool.tcp_servers_pool:
                        ServerPool.tcp_servers_pool[port][0] = ul
                        ServerPool.tcp_servers_pool[port][1] = dl
                    else:
                        ServerPool.tcp_servers_pool[port] = [0,0]
                        ServerPool.tcp_servers_pool[port][0] = ul
                        ServerPool.tcp_servers_pool[port][1] = dl
            except:
                continue

    @staticmethod
    def get_instance():
        if ServerPool.instance is None:
            ServerPool.instance = ServerPool()
        return ServerPool.instance

    def server_is_run(self, port):
        if port in self.tcp_servers_run:
            return True
        return False

    def new_server(self, port, password):
        ret = True
        port = int(port)
        logging.info("del server at %d" % port)
        try:
            udpsock = socket(AF_INET, SOCK_DGRAM)
            udpsock.sendto('%s:%s:%s:1' % (Config.MANAGE_PASS, port, password),
                           (Config.MANAGE_BIND_IP, Config.MANAGE_PORT))
            self.tcp_servers_run[port] = None
            udpsock.close()
        except Exception, e:
            logging.warn(e)
        return True

    def del_server(self, port):
        port = int(port)
        logging.info("del server at %d" % port)
        try:
            udpsock = socket(AF_INET, SOCK_DGRAM)
            udpsock.sendto('%s:%s:0:0' % (Config.MANAGE_PASS, port), (Config.MANAGE_BIND_IP, Config.MANAGE_PORT))
            del self.tcp_servers_run[port]
            udpsock.close()
        except Exception, e:
            logging.warn(e)
        return True

    def get_servers_transfer(self):
        ret = {}
        for port in self.tcp_servers_pool.keys():
            ret[port] = [self.tcp_servers_pool[port][0], self.tcp_servers_pool[port][1]]
        return ret