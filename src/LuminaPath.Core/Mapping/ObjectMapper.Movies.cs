using LuminaPath.Core.Dtos;
using LuminaPath.Core.Models;
using LuminaPath.Core.Models.Base;

namespace LuminaPath.Core.Mapping;

public partial class ObjectMapper
{
    private object MapMovieToDestination(Movie source, Type destinationType)
    {
        var destination = CreateInstance(destinationType);
        CopyMatchingProperties(source, destination);

        if (destination is Movie movie)
        {
            NormalizeMovieRelationships(source, movie);
        }

        return destination;
    }

    private static Movie MapMovie(MoviesNoIncludeDto source)
    {
        return new Movie
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genres = SplitGenres(source.Genre),
            ReleaseDate = source.ReleaseDate,
            ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
            Source = source.Source,
            Image = MapDocument<MediaDocument>(source.Image)
        };
    }

    private Movie MapMovie(MoviesDto source)
    {
        return new Movie
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genres = SplitGenres(source.Genre),
            ReleaseDate = source.ReleaseDate,
            ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
            Image = MapDocument<MediaDocument>(source.Image),
            MyMovies = source.MyMovies == null ? null : [Map<MyMovie>(source.MyMovies)]
        };
    }

    private MoviesNoIncludeDto MapMoviesNoIncludeDto(Movie source)
    {
        return new MoviesNoIncludeDto
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genre = JoinGenres(source.Genres),
            ReleaseDate = source.ReleaseDate,
            ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
            Source = source.Source,
            Image = MapDocument<Document>(source.Image)
        };
    }

    private MoviesDto MapMoviesDto(Movie source)
    {
        return new MoviesDto
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Genre = JoinGenres(source.Genres),
            ReleaseDate = source.ReleaseDate,
            ExpectedWatchTimeMinutes = source.ExpectedWatchTimeMinutes,
            Image = MapDocument<Document>(source.Image),
            MyMovies = source.MyMovies == null ? null : Map<MyMovieDto>(source.MyMovies.FirstOrDefault())
        };
    }

    private MyMovie MapMyMovie(MyMovieDto source)
    {
        return new MyMovie
        {
            Id = source.Id,
            Rating = source.Rating,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = source.TimeSpend,
            MovieId = source.MovieId,
            Movie = source.Movie == null ? null : Map<Movie>(source.Movie),
            CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes
        };
    }

    private MyMovie MapMyMovie(MyMovie source)
    {
        return new MyMovie
        {
            Id = source.Id,
            Rating = source.Rating,
            Priority = source.Priority,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = source.TimeSpend,
            LuminaUserId = source.LuminaUserId,
            MovieId = source.MovieId,
            CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes
        };
    }

    private MyMovieDto MapMyMovieDto(MyMovie source)
    {
        return new MyMovieDto
        {
            Id = source.Id,
            Rating = ToDtoRating(source.Rating),
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Status = source.Status,
            TimeSpend = ToDtoTimeSpend(source.TimeSpend),
            MovieId = source.MovieId,
            Movie = source.Movie == null ? null : Map<MoviesNoIncludeDto>(source.Movie),
            CurrentWatchTimeMinutes = source.CurrentWatchTimeMinutes
        };
    }

    private void NormalizeMovieRelationships(Movie source, Movie destination)
    {
        if (source.MyMovies == null)
        {
            destination.MyMovies = null;
            return;
        }

        destination.MyMovies = source.MyMovies
            .Select(myMovie =>
            {
                var mappedMyMovie = Map<MyMovie>(myMovie);
                mappedMyMovie.Movie = destination;
                mappedMyMovie.MovieId = destination.Id > 0 ? destination.Id : 0;
                return mappedMyMovie;
            })
            .ToList();
    }
}
