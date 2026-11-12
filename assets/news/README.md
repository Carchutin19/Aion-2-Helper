# Official game news reader

The Helper reads public Global and Korean notices, patch notes and current events directly from NCSOFT. It needs no account, Python service, game process, packet capture or browser engine. Article text is published in its original source language; Helper controls follow the selected application language. Global supports English, Spanish, German and French; Arabic controls use the English Global source.

## Cache and read state

Data is separate from overlay preferences under `%LOCALAPPDATA%\Aion2Helper\news`. Feed metadata is revalidated after 15 minutes, article bodies after 24 hours and images after 7 days. Manual refresh is limited to once per 30 seconds. Network errors retain cached feeds and articles. Images are decoded at bounded size, with at most two parallel downloads; hidden news pages do not request thumbnails. Disk cache is trimmed to 64 MiB and image memory cache to 24 MiB. Article/thumbnail response limits are 2/4 MiB respectively. No article read state or cache belongs in a published support report or release archive.

`state.json` stores selected region and read article revisions. An article is marked read when opened, or by the regional Mark all as read button. Changing region, refreshing or viewing the list does not mark articles read. Server view counts are not used as revisions.

Advanced source configuration: copy `sources.example.json` to that cache folder as `sources.json` while the Helper is closed. HTTPS official source hosts and redirects are validated. The interface shows a source-unavailable notice and retained data if an endpoint changes.

## Native article reader

The reader supports text, headings, lists, inline images, links and tables. It does not execute scripts, CSS, forms, videos or frames. Extremely large documents have explicit rendering limits and direct users to the original announcement when exceeded. Event promotion pages can contain interactive experiences; their detail view shows the banner and dates and links to the official rules and rewards. Every item includes its original official link. Dates are converted from the source timestamp to the user’s local time.

## Attribution

Editorial content and artwork: NCSOFT and respective rights holders. Protocol of the public endpoints was researched using axsddlr/aion2_api (MIT), with a separately written C# reader. HTML parsing uses the embedded Html Agility Pack 1.13.1 (MIT). See `THIRD-PARTY.md` and `assets/licenses`.
