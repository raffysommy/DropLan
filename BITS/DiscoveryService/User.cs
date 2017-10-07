using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Net;
using BITS.JsonHelper;

namespace BITS.DiscoveryService
{
    public class User
    {
        private static User _currentuser;
        public static User Currentuser { get => _currentuser; internal set => _currentuser = value; }

        private String _username;
        private Image _userimage;
        private IPEndPoint _useraddr;
        private Guid _id;

        public string Username { get => _username; set => _username = value; }
        [JsonConverter(typeof(JsonHelper.ImageConverter))]
        public Image Userimage { get => _userimage; set => _userimage = value; }
        [JsonConverter(typeof(IPEndPointConverter))]
        public IPEndPoint Useraddr { get => _useraddr; set => _useraddr = value; }
        public Guid Id { get => _id; set => _id = value; }

        /// <summary>
        /// Costructor with field assigment.
        /// </summary>
        /// <param name="name">Username</param>
        /// <param name="img">Profile Image of User</param>
        /// <param name="ip">IP endpoint(IP and port) of the user</param>
        public User(String name, Image img, IPEndPoint ip)
        {
            Userimage = img;
            Username = name;
            Useraddr = ip;
            _id = Guid.NewGuid();
        }
        /// <summary>
        /// Costructor with field and id assigment.
        /// </summary>
        /// <param name="name">Username</param>
        /// <param name="img">Profile Image of User</param>
        /// <param name="ip">IP endpoint(IP and port) of the user</param>
        /// <param name="id">Unique ID</param>
        public User(String name,Image img,IPEndPoint ip,Guid id) :this(name, img, ip) {
            _id = id;
        }
        /// <summary>
        /// Deserializer Costructor
        /// </summary>
        /// <param name="json">Json string that contain the object</param>
        public User(String json)
        {
            User tmp = JsonConvert.DeserializeObject<User>(json);
            Username = tmp.Username;
            Userimage = tmp.Userimage;
            Useraddr = tmp.Useraddr;
            Id = tmp.Id;
        }
        /// <summary>
        /// Void Costructor
        /// </summary>
        public User(){
        }
        /// <summary>
        /// General equals istruction that try to cast the object.
        /// </summary>
        /// <param name="obj">The other object</param>
        /// <returns>Equality boolean</returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as User);
        }
        public bool Equals(User other) {
            if (other != null) {
                if (other.Id != null) {
                    return other.Id.Equals(this.Id);
                }
            }
            return false;
        }
        public override int GetHashCode() {
            return Id.GetHashCode();
        }

        /* OLD implementation
        /// <summary>
        /// Compare two user and return true if there are equals.
        /// </summary>
        /// <param name="other">The other User</param>
        /// <returns>True: Two users are the same object. False: Two users are different</returns>
        public bool Equals(User other) {
            if (other != null) {
                if (other.Username != null || other.Useraddr != null || other.Userimage != null) {
                    return Username.Equals(other.Username) && Useraddr.Address.ToString().Equals(other.Useraddr.Address.ToString()) && Useraddr.Port.ToString().Equals(other.Useraddr.Port.ToString()) && JsonHelper.ImageConverter.Compare2Image(Userimage,other.Userimage);
                }
            }
            return false;
        }
        /// <summary>
        /// Combine the tree Hashcode with a XOR operation;
        /// </summary>
        /// <returns>Hash Code of User</returns>
        public override int GetHashCode()
        {
            return Username.GetHashCode() ^ JsonHelper.ImageConverter.HashImage(Userimage) ^ Useraddr.Address.ToString().GetHashCode()^Useraddr.Port.ToString().GetHashCode();  
        }*/

        /// <summary>
        /// Convert Object in a JSON Rappresentation
        /// </summary>
        /// <returns>JSON String of converted object</returns>
        public String ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}