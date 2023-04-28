# GitLab symbol server proxy

[Available from DockerHub](https://hub.docker.com/r/peeveen/gitlabsymbolserverproxy)

Recently, GitLab allowed `snupkg` files to be pushed to the internal NuGet registry. This was great news!

However, [GitLab itself does not yet function as a symbol server](https://gitlab.com/gitlab-org/gitlab/-/issues/342157), so nothing can really consume the symbol information in those packages. This is terrible news!

This project is a webservice that can run alongside GitLab and serve the symbol information from those packages.

## How it works

In the Visual Studio Options dialog (Tools → Options), add the URL of this webservice to the list of symbol servers (Debugging → Symbols).

When debugging, Visual Studio will now start asking this webservice for PDBs that it cannot find locally.

These requests will contain a name (e.g. `Foo.Bar.pdb`) and a hex-string "hash" that serves as a sort-of version identifier.

1. The webservice will search within the package registries of all projects in all top-level GitLab groups for packages that match the name `Foo.Bar`.
2. From these, it will look for any files in that package that have a `.snupkg` extension.
3. Then it will download these `.snupkg` files and extract any PDB files. These will be cached locally to improve future performance.
4. An internal GUID is read from each PDB, and used to create a "hash" string.
5. If any of these match the "hash" part of the request, then the matching PDB is streamed back to Visual Studio.

## Build

1. _(Optional)_ Modify `appsettings.yml` with your preferred settings (see 'Usage' section below). If you don't do this, you will have to supply your configuration arguments via command line when you run the proxy.
2. Run the build command (you can specify a different image tag if you wish):

```
docker build -t gitlabsymbolserverproxy .
```

> You can add `--build-arg version=n.n.n.n` to set the version numbers in the built files, otherwise they will have a default version of 1.0.0.0.

## Running the unit tests

If you have the .NET SDK installed:

```
dotnet test
```

... or if you want to use Docker:

```
docker run -v ${PWD}/:/GitLabSymbolServerProxy mcr.microsoft.com/dotnet/sdk /bin/sh -c "cd GitLabSymbolServerProxy && dotnet test"
```

## Run

Assuming you have used the suggested tag, you can run your built image with this command (mapped port numbers can obviously be changed if you wish):

```
docker run -dit -p 5043:80 -p 5044:443 gitlabsymbolserverproxy
```

> See the upcoming 'Usage' section for available arguments.

If you want to quickly check that the app is running, there is `/version` endpoint that will return the app version:

```
# Via HTTP
curl http://localhost:5043/version
# Via HTTPS ... add -k if you are using a self-signed certificate
curl https://localhost:5044/version
```

## Usage

Arguments can be supplied by appending them to the `docker run` command (using `--ArgumentName=Value` syntax), or by setting their values in the appropriate `appsettings.*.yml` file:

Arguments specific to this app are:

- `GitLabHostOrigin`: _(required)_ The origin of the GitLab host (e.g. https://gitlab.yourdomain.com)
- `PersonalAccessToken`: _(required)_ A personal access token that will be used to access the package registries. This token must have at least `read_api` scope.
- `UserName`: _(required)_ The name of the user that the access token is associated with. This is needed because the authentication on the GitLab NuGet API requires a username to provided.
- `CacheRootPath`: _(required)_ Path where downloaded PDBs will be cached. If this path does not exist, it will be created, if possible.
- `SupportedPdbNames`: _(optional)_ A collection of regular expressions. Any request for a PDB that does not match any of these regular expressions will be ignored (a 404 response will be returned to Visual Studio). This allows your proxy to save time by not searching GitLab for packages that you definitely won't have.

Any other property from the `appsettings.yml` file can also be supplied via command line. Nested properties should be separated by colon characters, e.g. `--TopLevelProperty:NestedProperty:FurtherNestedProperty=Value`.

## TODO

- Unit tests!
- Work with native PDBs (currently only portable supported).
