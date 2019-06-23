using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using SciterSharp;

namespace StickyNotes
{
	class Temp
	{
		public int Id { get; set; }
		public int level;
	}

	class Flashcard
	{
		public int Id { get; set; }
		public DateTime? dt_last_review { get; set; }
		public int level { get; set; }
		public string front { get; set; }
		public string back { get; set; }
	}

	class Deck
	{
		public int Id { get; set; }
		public DateTime dt_start { get; set; }
		public string name { get; set; }

		[BsonRef]
		public List<Flashcard> cards { get; set; }
	}

	class SpaceRepetition : LiteDatabase
	{
		// tables
		private LiteCollection<Deck> _db_decks;
		private LiteCollection<Flashcard> _db_cards;

		private DateTime _now = DateTime.Now.Date;

		public SpaceRepetition()
			: base(@"D:\ProjetosSciter\StickyNotes\StickyNotes\decks.db")
		{
			_db_decks = GetCollection<Deck>();
			_db_cards = GetCollection<Flashcard>();

			if(false)
			{
				DropCollection(_db_decks.Name);
				DropCollection(_db_cards.Name);
				Test();
			}
		}

		public List<Deck> GetDecks()
		{
			return _db_decks.IncludeAll().FindAll().ToList();
		}

		private void Test()
		{
			var demo_cards = new List<Flashcard>()
			{
				new Flashcard() { front = "Roraima", back = "Boa Vista" }
			};
			_db_cards.Insert(demo_cards);

			var deck = new Deck()
			{
				dt_start = DateTime.Now,
				name = "Estados e Capitais",
				cards = demo_cards
			};
			_db_decks.Insert(deck);

			var card = demo_cards[0];

			// day 0
			_now = _now.AddDays(0);
			Debug.Assert(TodayCards().Count(c => c.Id == card.Id) == 1);
			ReviewAttempt(card.Id, true);

			// day 2
			_now = _now.AddDays(2);
			Debug.Assert(TodayCards().Count(c => c.Id == card.Id) == 1);
			ReviewAttempt(card.Id, true);

			// day 4
			_now = _now.AddDays(4);
			Debug.Assert(TodayCards().Count(c => c.Id == card.Id) == 1);
			ReviewAttempt(card.Id, true);

			// day 8
			_now = _now.AddDays(8);
			Debug.Assert(TodayCards().Count(c => c.Id == card.Id) == 1);
			ReviewAttempt(card.Id, false);

			// day 9
			_now = _now.AddDays(1);
			Debug.Assert(TodayCards().Count(c => c.Id == card.Id) == 1);
			ReviewAttempt(card.Id, true);
		}

		public void ReviewAttempt(int card_id, bool correct)
		{
			Debug.Assert(TodayCards().Count(c => c.Id == card_id) == 1);

			var card = _db_cards.FindById(card_id);
			card.dt_last_review = _now;
			if(correct)
				card.level++;
			else
				card.level = 0;
			_db_cards.Update(card);
		}

		public List<Flashcard> TodayCards()
		{
			var dt_today = _now.Date;
			var res = new List<Flashcard>();

			foreach(var deck in _db_decks.IncludeAll().FindAll())
			{
				foreach(var card in deck.cards)
				{
					DateTime dt_review = CardReviewDate(card);
					if(dt_today >= dt_review)
						res.Add(card);
				}
			}
			return res;
		}

		public void AddCard(string front, string back)
		{
			var card = new Flashcard()
			{
				front = front,
				back = back,
				level = 0
			};
			_db_cards.Insert(card);

			var deck = _db_decks.FindAll().First();
			deck.cards.Add(card);
			_db_decks.Update(deck);
		}

		private DateTime CardReviewDate(Flashcard card)
		{
			int gap = CardReviewDaysGap(card);
			if(gap == 0)
				return card.dt_last_review == null ? _now : card.dt_last_review.Value.AddDays(1);
			return card.dt_last_review.Value.AddDays(gap);
		}

		private int CardReviewDaysGap(Flashcard card)
		{
			// level 0: every day
			// level 1: every 2 day
			// level 2: every 4 day
			// level 3: every 8 day
			// level 4: every 16 day
			// level 5: every 32 day
			if(card.level == 0)
				return 0;

			return (int)Math.Pow(2, card.level);
		}
	}
}