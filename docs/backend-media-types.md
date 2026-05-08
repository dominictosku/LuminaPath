# Adding a New Media Type

LuminaPath models media in two layers:

- `Media`: the catalog item, such as a game, movie, or anime.
- `MyMedia`: the user-owned library item that points to the catalog item.

For a new media type, create both concrete models:

```csharp
public class Anime : Media
{
    public List<MyAnime>? MyAnimes { get; set; }
}

public class MyAnime : MyMedia, IMyMedia
{
    [NotMapped]
    public int MediaId => AnimeId;

    public int AnimeId { get; set; }
    public Anime? Anime { get; set; }
}
```

Then add services by inheriting from the media bases:

```csharp
public class AnimeService : MediaModelService<Anime, MyAnime>
{
    protected override IQueryable<Anime> IncludeUserLibrary(IQueryable<Anime> query, string userId)
        => query.Include(anime => anime.MyAnimes!.Where(item => item.LuminaUserId == userId));

    protected override Expression<Func<Anime, bool>> IsInUserLibrary(string userId)
        => anime => anime.MyAnimes != null && anime.MyAnimes.Any(item => item.LuminaUserId == userId);
}

public class MyAnimeService : UserMediaModelService<MyAnime, Anime>
{
    protected override Func<IQueryable<MyAnime>, IOrderedQueryable<MyAnime>> DefaultOrderBy
        => query => query.OrderByDescending(item => item.Anime!.ReleaseDate);

    protected override Expression<Func<MyAnime, bool>> HasMediaId(int mediaId)
        => item => item.AnimeId == mediaId;
}
```

The controller can reuse the shared catalog behavior:

```csharp
public class AnimesController : MediaController<Anime, AnimeDto, AnimeService, MyAnime>
{
    public AnimesController(AnimeService service, IObjectMapper mapper)
        : base(service, mapper)
    {
        Includes = ["Image", "MyAnimes"];
    }
}
```

Finally, register the services, add `DbSet<Anime>` and `DbSet<MyAnime>`, create DTO mappings, and generate a migration.
