using ProtoBuf;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IA.Models
{
	[ProtoContract]
	internal class CommandState
    {
		[ProtoMember(1)]
        public string CommandName { get; set; }

		[ProtoMember(2)]
		public long ChannelId { get; set; }

		[ProtoMember(3)]
		public bool State { get; set; }
    }
}