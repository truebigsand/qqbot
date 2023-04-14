using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chaldene.Data.Messages;
using Chaldene.Sessions;

namespace qqbot
{
    public delegate Task GroupCommandHandler(MiraiBot bot, Chaldene.Data.Messages.Receivers.GroupMessageReceiver e, string[] args);
    public class GroupCommandHelper
    {
        private Dictionary<string, GroupCommandHandler> CommandMap;
        public GroupCommandHelper()
        {
            CommandMap = new Dictionary<string, GroupCommandHandler>();
        }
        public GroupCommandHelper(IEnumerable<KeyValuePair<string, GroupCommandHandler>> CommandMap)
        {
            this.CommandMap = new(CommandMap);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <exception cref="ArgumentException"></exception>
        public void AddCommand(string name, GroupCommandHandler handler)
        {
            if (!CommandMap.ContainsKey(name))
            {
                throw new ArgumentException("command exists!", name);
            }
            CommandMap.Add(name, handler);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public GroupCommandHandler GetHandler(string name)
        {
            if (!CommandMap.TryGetValue(name, out GroupCommandHandler handler))
            {
                throw new KeyNotFoundException("command not found!");
            }
            return handler;
        }
    }
}
