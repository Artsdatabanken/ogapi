# ogapi

API for økologi

* [API](https://ninmemapi.artsdatabanken.no/)
* [API documentation](https://ninmemapi.artsdatabanken.no/swagger/)

## Relaterte prosjekter

* [Innsynsklient](https://github.com/artsdatabanken/ratatouille)
* [REST API](https://github.com/Artsdatabanken/ogapi)
* [GIS Dataflyt](https://github.com/artsdatabanken/grunnkart-dataflyt)
* [Metadataflyt](https://github.com/artsdatabanken/kverna)
* [Geografisk API](https://github.com/Artsdatabanken/rasterQ) og [Deployment](https://github.com/Artsdatabanken/rasterUploader) av dette
* Verktøy for [rødlisting av naturtyper](https://github.com/Artsdatabanken/natty)

## Konvensjonelt API

...

## Dyanmisk Graph API

Graph-delen av APIet har 2 operasjoner på går mot grafdatabasen i CosmosDB. Denne har fokus på taxon traits som er hentet fra [EoL](http://eol.org/). Den inkluderer arter som har EoL-traits, samt deres foreldre. Årsaken til dette er tiden det tar å få lagt inn data, siden CosmosDB må sies å være et nokså umodent produkt på dette tidspunktet. CosmosDBs "killer feature" er i denne sammenhengen "out of the box"-støtte for spørrespråket [Gremlin](https://github.com/tinkerpop/gremlin/wiki), som omtales om litt. Det er også fort gjort å sette opp CosmosDB i Azure. Ved ønske om å bruke mer tid på et oppsett med andre muligheter, er [JanusGraph](http://janusgraph.org/) et aktuelt alternativ.

I tillegg til taxons med traits, er rødlister, svartelister, miljøvariabler, beskrivelsesvariabler og naturområdetyper inkludert.

Operasjonen 'Graf' tillater dynamiske Gremlin-spørringer, som f.eks. `g.V().hasLabel('art').count()`, som teller opp hvor mange art-vertices som finnes i basen. Et annet eksempel er: `g.V('ar_43049').in('spiser')` -> Hvem som spiser arten med ScientificNameId == 43049. In og Out er forklart kort i operasjonens dokumentasjon. Out er vertexen relasjonen (edgen) går fra. In er vertexen relasjonen går til. Man starter alltid med 'g' for graf, og enten `V()` for vertices, eller `E()` for edges. `g.V()` velger alle vertices. Det er med andre ord en fordel å filtrere ... `g.V('ar_43049')` velger kun én vertex, med angitt id. `g.V().has('name', 'Zoarces viviparus')` velger samme vertex, men filtrert på egenskapen 'name'. Merk at taxons har både egenskapen 'name' og 'navn'. Årsaken til det er at 'name' er en "standardegenskap" og brukes av CosmosDBs visualiseringsverktøy, mens 'navn' er en json med støtte for navn på flere språk, f.eks. `{"la":"Asteroidea","nb":"Sjøstjerner"}`. Denne egenskapen bærer bare data og egner seg ikke for filtrering.

Operasjonen 'Relasjoner' henter relasjonene inn og ut for en angitt kode. Prøv f.eks. `ar_3560`, som har litt data på seg. Man kan velge detaljert visning for å få med mer om vertices og edges. Denne operasjonen passer til å f.eks. populere informativ metadata for en art man er inne på i klienten.

### Om Gremlin

Gremlin er et "fluent" og funksjonelt spørrespråk som fungerer utmerket til traversering av en graf, det vil si å nøste opp i relasjoner av relasjoner (osv. ->) av data av interesse. Jeg anbefaler følgende tutorial for å lære litt mer: http://tinkerpop.apache.org/docs/current/tutorials/getting-started/ Hvis man prøver å se for seg hvordan man skulle ha gjort en lignende nøsting med SQL, vil nok Gremlins verdi fort åpenbare seg, både for lesbarhet og kompleksitet.

### CosmosDB

Det anbefales å leke litt med visualiseringsverktøyet til CosmosDB, som man finner ved å gå inn på 'adb-graph' i Azure, velge 'Data Explorer', ekspandere `adb-og-graph-db-v4` og `abd-og-graph-v4`, og velge 'Graph'. Der vil man se spørringen 'g.V()', som man kan redigere, og få visualisert vertices og edges. Merk at spørringene her vil være betydelig tyngre og tregere enn når man kjører dem via operasjonen 'Graf'. Dette skyldes ekstraarbeid CosmosDB gjør for å nøste opp i det som trengs til visualiseringen. Prøv f.eks. noe så enkelt som `g.V('na_l')`.
