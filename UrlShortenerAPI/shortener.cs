using Model;
public class URLshort
{
	// En intern metod för att förkorta en URL och generera en unik nanoid
	internal async Task<string> Shorten(string fullURL, string HOSTNAME, UrlContext db)
	{
		// Generera en unik nanoid
		var nanoid = await Nanoid.Nanoid.GenerateAsync(size: 7);

		// Skapa ett URL-objekt med den genererade nanoid och den fullständiga URL:en
		var url = new URL()
		{
			nanoid = nanoid,
			url = fullURL
		};

		// Lägg till URL-objektet i databasen
		await db.URLs.AddAsync(url);
		await db.SaveChangesAsync();

		// Bygg den förkortade URL:en med nanoid och HOSTNAME
		var shortURL = BuildUrl(nanoid, HOSTNAME);
		return shortURL;
	}

	// En intern metod för att bygga en förkortad URL med nanoid och HOSTNAME
	internal string BuildUrl(string id, string HOSTNAME)
	{
		string shortURL = HOSTNAME + id;
		return shortURL;
	}

}