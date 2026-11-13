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


## Helper releases and translation

A fourth tab shows the latest normal published Helper release from GitHub, with cached content and unread note revisions, independently of game region. Release notes retain their published English text. Drafts/prereleases and foreign release links are rejected.

Korean game articles remain Korean in the native reader. Translate in browser opens Google Translate for the original official URL and chosen Helper language. No API key or automatic translation request is used; availability depends on the provider/site.

Cards inspect up to four allowlisted thumbnails, skipping decorative strips. Event banners and full article images retain their original handling. Missing/loading/failed card images use the embedded cover below.

# Default news cover

`fallback.jpg` is a format conversion of the Aion 2 illustration supplied by the
project maintainer as `Aion_2_Ascension_Gauge_Guide_96c65346ba.webp` on
November 13, 2026. The composition and source dimensions (860 × 484) are retained.
Original SHA-256: `635f0eaf11db9cddb3b9d2d2af0dcb18431e0559392d4a42576e211bbe548bdc`.

It is embedded in the executable and used only when a game-news card has no
usable article image, while an image is loading, or when loading fails. The
shared frozen drawing preserves the entire cover inside a themed frame. It
requires no image download and allocates no separate bitmap per article.

Aion 2 artwork, names and trademarks remain the property of NCSOFT and their
respective owners. The Helper software licence does not relicense this artwork.
