using Model;
public class URLshort
{
	// En intern metod f�r att f�rkorta en URL och generera en unik nanoid
	internal async Task<string> Shorten(string fullURL, string HOSTNAME, UrlContext db)
	{
		// Generera en unik nanoid
		var nanoid = await Nanoid.Nanoid.GenerateAsync(size: 7);

		// Skapa ett URL-objekt med den genererade nanoid och den fullst�ndiga URL:en
		var url = new URL()
		{
			nanoid = nanoid,
			url = fullURL
		};

		// L�gg till URL-objektet i databasen
		await db.URLs.AddAsync(url);
		await db.SaveChangesAsync();

		// Bygg den f�rkortade URL:en med nanoid och HOSTNAME
		var shortURL = BuildUrl(nanoid, HOSTNAME);
		return shortURL;
	}

	// En intern metod f�r att bygga en f�rkortad URL med nanoid och HOSTNAME
	internal string BuildUrl(string id, string HOSTNAME)
	{
		string shortURL = HOSTNAME + id;
		return shortURL;
	}

}