import time
import sys
import thread
import server_pool
import db_transfer
import restful
import logging

logging.basicConfig(level=logging.INFO)

#def test():
#    thread.start_new_thread(DbTransfer.thread_db, ())
#    Api.web_server()

if __name__ == '__main__':
    #server_pool.ServerPool.get_instance()
    #server_pool.ServerPool.get_instance().new_server(2333, '2333')
    thread.start_new_thread(db_transfer.DbTransfer.thread_db, ())
    restful.run_web_server()
