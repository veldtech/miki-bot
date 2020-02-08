namespace Miki.API.Cards.Objects
{
    using ProtoBuf;
    using System.Collections.Generic;
    
    [ProtoContract]
	public class CardHand
	{
		[ProtoMember(1)]
		public List<Card> Hand = new List<Card>();

		public void AddToHand(Card card)
		{
			Hand.Add(card);
		}

		public void ShowAll()
		{
			foreach(var card in Hand)
			{
				card.isPublic = true;
			}
		}

		public string Print()
		{
			string output = "";
			Hand.ForEach((x) => output += x.ToString());
			return output;
		}
	}
}