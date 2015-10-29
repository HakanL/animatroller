using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Util
{
    public class GroupControlToken : IControlToken
    {
        internal Dictionary<IOwnedDevice, IControlToken> MemberTokens { get; set; }
        private Action disposeAction;

        public GroupControlToken(Dictionary<IOwnedDevice, IControlToken> memberTokens, Action disposeAction, int priority = 1)
        {
            MemberTokens = memberTokens;
            this.disposeAction = disposeAction;
            Priority = priority;
        }

        public int Priority { get; set; }

        public void Dispose()
        {
            foreach (var memberToken in MemberTokens.Values)
                memberToken.Dispose();
            MemberTokens.Clear();

            this.disposeAction();
        }

        public void PushData(DataElements dataElement, object value)
        {
            foreach (var memberToken in MemberTokens.Values)
                memberToken.PushData(dataElement, value);
        }

        public bool IsOwner(IControlToken checkToken)
        {
            return MemberTokens.ContainsValue(checkToken);
        }
    }
}
