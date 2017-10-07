using NLog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BITS.DiscoveryService
{
    public delegate void UserChange(User user);
    public class DiscoveryUserLocal
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private User _myuser;
        private List<User> _listuser=new List<User>();
        private Dictionary<User, DateTime> _updateTime=new Dictionary<User, DateTime>();
        private UDPMulticast _udpm;
        Boolean _privatemode;
        public event UserChange _effectiveUserAdd;
        public event UserChange _effectiveUserRemove;

        public List<User> Listuser { get => _listuser; private set => _listuser = value; }
        public bool Privatemode {
            private get => _privatemode;
            set {
                lock (this)
                {
                    _privatemode = value;
                    Monitor.PulseAll(this);
                }
            }
        }
        /// <summary>
        /// Discover and announcement service.
        /// </summary>
        private void Discover() {
            try
            {
                _udpm = new UDPMulticast("239.0.0.222", 3000);
                _udpm._asyncUserAdd += Udpm__asyncUserAdd;
                while (true)
                {
                    if (!Privatemode)
                    {
                        try
                        {
                            _udpm.SendNotification(_myuser);
                        }
                        catch(Exception e) {
                            logger.Warn("Unable to send periodical notification: " + e.Message);
                        } //we can just ignore if we cannot send notification.
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        lock (this)
                        {
                            Monitor.Wait(this);
                        }
                    }
                }
            }
            catch(Exception e) {
                logger.Error("Discovery server error:" + e.Message);
                throw;
            }
        }
        /// <summary>
        /// Add user to the list or update timestamp.
        /// </summary>
        /// <param name="user">User to add</param>
        private void Udpm__asyncUserAdd(User user)
        {
            lock (Listuser)
            {
                if (!_myuser.Equals(user))
                {
                    if (Listuser.Contains(user))
                    {
                        _updateTime[user] = DateTime.UtcNow;
                    }
                    else
                    {
                        _updateTime[user] = DateTime.UtcNow;
                        Listuser.Add(user);
                        _effectiveUserAdd?.Invoke(user);
                    }
                }
            }            
        }
        /// <summary>
        /// Garbage collector for inactive user
        /// </summary>
        private void GarbageCollector() {
            while (true)
            {
                lock (Listuser)
                {
                    Listuser.RemoveAll((User u) =>
                     {
                         if ((DateTime.UtcNow - _updateTime[u]).TotalSeconds > 30)
                         {
                             _updateTime.Remove(u);
                             _effectiveUserRemove?.Invoke(u);
                             return true;
                         }
                         return false;
                     });
                }
                Thread.Sleep(30000);
            }
        }
        public DiscoveryUserLocal(User user,Boolean privateMode=false){
            User.Currentuser = user;
            _myuser = User.Currentuser;
            _privatemode = privateMode;
            Thread background_discover = new Thread(new ThreadStart(Discover))
            {
                IsBackground = true
            };
            background_discover.Start();
            Thread garbage_thread = new Thread(new ThreadStart(GarbageCollector))
            {
                IsBackground = true
            };
            garbage_thread.Start();
        }

    }
}
