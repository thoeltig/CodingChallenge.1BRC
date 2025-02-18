# The One Billion Row Challenge

At the beginning of January 2024 Gunnar Morling launched The One Billion Row Challenge (1BRC) to the Java Community on his [blog](https://www.morling.dev/blog/one-billion-row-challenge/) and [GitHub](https://github.com/gunnarmorling/1brc). The challenge aimed to find Java code that processed one billion rows from a text file and aggregate the values in the fastest time possible. A lot of developers submitted their solutions until Morling closed the challenge at the end of the month and published the [final leaderboards](https://www.morling.dev/blog/1brc-results-are-in/).
After the news of this challenge spread many people built their own solutions using other [languages, databases, and tools](https://github.com/gunnarmorling/1brc/discussions/categories/show-and-tell).

## Basics of running the challenge

1. Generate the measurements file with 1B rows (just once).
	- **Attention:** This will take a few minutes and the generated file has a size of approx. **12 GB**, so make sure to have enough diskspace.
2. Calculate the min, max & average values from the measurements file.
	- Measure the time needed for reading the file and calculating the average. Output of the result values is not part of the challenge. 
3. Optimize the heck out of it to speed up your code!

## Rules and limits

- No external library dependencies may be used.
- Implementations must be provided as a single source file.
- The computation must happen at application _runtime_, i.e. you cannot process the measurements file at _build time_ and just bake the result into the binary.
- Input value ranges are as follows:
    - Station name: non null UTF-8 string of min length 1 character and max length 100 bytes, containing neither `;` nor `\n` characters. (i.e. this could be 100 one-byte characters, or 50 two-byte characters, etc.).
    - Temperature value: non null double between -99.9 (inclusive) and 99.9 (inclusive), always with one fractional digit.
- There is a maximum of 10,000 unique station names.
- Line endings in the file are `\n` characters on all platforms.
- Implementations must not rely on specifics of a given data set, e.g. any valid station name as per the constraints above and any data distribution (number of measurements per station) must be supported.
- The rounding of output values must be done using the semantics of IEEE 754 rounding-direction "roundTowardPositive".

## Setup
Old notebook - specs later

## Writing & reading a file
There are a couple of classes that could be used to read & write data to & from a file:
- _File.Create_ returns a _FileStream_ with a buffer size of 4096 and _FileOptions.None_.
- _File.CreateText_ & _FileInfo.CreateText_ returns a _StreamWriter_ with a buffer size of 1024 and _FileOptions.SequentialScan_.
- _File.ReadAllText_ uses a _StreamReader_ with a buffer size of 1024 and _FileOptions.SequentialScan_.
- _StreamWriter_ & _StreamReader_ are basically an additional buffer with default size 1024 added on top of an internal _FileStream_ with buffer size 4096.
	- _StreamWriter_ is able to write objects, different value types, strings and char arrays to a file.
	- Under the hood every input is converted to a string and then again converted to char array. 
	- These char arrays fill the buffer and if it is full the buffer is converted to a byte array and written to the internal _FileStream_.
	- _StreamReader_ reads all bytes from a file and converts them to a string.
- Every way to access a file utilizes a _FileStream_ which can only read & write bytes.

> [!NOTE]
> Previously done performance measurements for the other classes can be found [here](https://github.com/thoeltig/CodingChallenge.1BRC/blob/c61026cbed6723086e31529bdb0de293816e4264/README.md#writing).

Simple explanation of the internal logic of a _FileStream_:
- Has an internal buffer which holds the bytes. If the buffer is full it is written to the file system cache.
	- This can be forced by calling _FileStream.Flush_. This is also called when disposing the _FileStream_.
- The file system writes the data lazily to disk depending on the hard disk write speed.
- Reading the bytes is similiar. Depending on the access type more or less is cached by the file systen which in turn allows for faster sequential paging through the data or faster access at random positions.
- Read & write have a couple of options which can be added when calling the methods. For this case the following are interesting:
	- _FileOptions.None_ will pass no additional access flags to the file system. So it will try to guess the optimal cache size depending on the access pattern.
	- _FileOptions.RandomAccess_ will cache less because it expects the file to be accessed at random positions by multiple applications.
	- _FileOptions.SequentialScan_ will cache more because it expects the file to be accessed sequentially by a single application.
	- _FileOptions.WriteThrough_ will write to file system cache but is flushed to disk without delay.
	- If _FILE_FLAG_NO_BUFFERING_ is used in combination with _FileOptions.WriteThrough_ the file system cache is ignored and the data is immediately flushed to disk.
		- This flag has a some of memory alignment requieremesnts and it isn't included in _FileOptions_ but can still be passed down.
	
> [!IMPORTANT]
> Additional informations on the topic:
> - [Win32.CreateFileA](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea)
> - [Win32.ReadFile](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile)
> - [Caching behaviour](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea#caching-behavior)
> - [File buffering And _FILE_FLAG_NO_BUFFERING_ requierments](https://learn.microsoft.com/en-us/windows/win32/fileio/file-buffering)

### Test
The test will use the _FileStream_ to write & read 20000000 bytes to & from a file:
- Buffer and block sizes
	- 1024
	- 2048
	- 4096
	- 8192
	- 16384
	- 32768
	- 65536
	- 131072
	- 262144
- Access types
	1. _FileOptions.None_
	2. _FileOptions.SequentialScan_
	3. _FileOptions.WriteThrough_
	4. _FileOptions.SequentialScan_ + _FileOptions.WriteThrough_
	5. _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_ 
	6. _FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_
- The elapsed time is the median of 12 tests as the fractional portion of a second.
	- The elapsed times of each test are sorted, first and last quarter is ignored to avoid using the extrem values and then the average is calcualted from the remaining half. 
- Executed code
	- Net Framework 4.7.2 Console with byte[]
	- Net. 5 (Core) Console with Spans<byte>
	
#### Net. 5 + Spans<byte> (Execution time 32 min)

|Write|1|%|2|%|3|%|4|%|5|%|6|%|
|----|----|----|----|----|----|----|----|----|----|----|----|----|
|Buffer 1024 Chunk 1024 (19532 times)|00:0834268|0|00:0779739|-0.07|08:9973465|106.85|08:8773825|105.41|-|-|-|-|
|Buffer 1024 Chunk 2048 (9766 times)|00:0597098|-0.28|00:0582217|-0.3|06:9542618|82.36|06:7610332|80.04|-|-|-|-|
|Buffer 1024 Chunk 4096 (4883 times)|00:0250798|-0.7|00:0375475|-0.55|05:6135621|66.29|04:9507096|58.34|-|-|-|-|
|Buffer 1024 Chunk 8192 (2442 times)|00:0165427|-0.8|00:0168215|-0.8|02:2649678|26.15|02:6136359|30.33|-|-|-|-|
|Buffer 1024 Chunk 16384 (1221 times)|00:0147957|-0.82|00:0130861|-0.84|01:3930413|15.7|01:5598440|17.7|-|-|-|-|
|Buffer 1024 Chunk 32768 (611 times)|00:0110859|-0.87|00:0118030|-0.86|00:9147307|9.96|01:1463434|12.74|-|-|-|-|
|Buffer 1024 Chunk 65536 (306 times)|00:0097368|-0.88|00:0123318|-0.85|00:7543126|8.04|00:7463461|7.95|-|-|-|-|
|Buffer 1024 Chunk 131072 (153 times)|00:0089515|-0.89|00:0091547|-0.89|00:4420882|4.3|00:5698328|5.83|-|-|-|-|
|Buffer 1024 Chunk 262144 (77 times)|00:0090553|-0.89|00:0088737|-0.89|00:3395642|3.07|00:4209115|4.05|-|-|-|-|
|Buffer 2048 Chunk 1024 (19532 times)|00:0490040|-0.41|00:0456591|-0.45|06:6876834|79.16|06:3833559|75.51|-|-|-|-|
|Buffer 2048 Chunk 2048 (9766 times)|00:0525519|-0.37|00:0468211|-0.44|06:2764692|74.23|06:7172033|79.52|-|-|-|-|
|Buffer 2048 Chunk 4096 (4883 times)|00:0367317|-0.56|00:0290311|-0.65|05:2840010|62.34|04:7544740|55.99|-|-|-|-|
|Buffer 2048 Chunk 8192 (2442 times)|00:0176592|-0.79|00:0168561|-0.8|02:1437175|24.7|02:5480506|29.54|-|-|-|-|
|Buffer 2048 Chunk 16384 (1221 times)|00:0137103|-0.84|00:0126863|-0.85|01:2383844|13.84|01:5516160|17.6|-|-|-|-|
|Buffer 2048 Chunk 32768 (611 times)|00:0114235|-0.86|00:0119374|-0.86|01:1146716|12.36|01:0885421|12.05|-|-|-|-|
|Buffer 2048 Chunk 65536 (306 times)|00:0112174|-0.87|00:0120796|-0.86|00:8139183|8.76|00:7657653|8.18|-|-|-|-|
|Buffer 2048 Chunk 131072 (153 times)|00:0095045|-0.89|00:0098604|-0.88|00:4525957|4.43|00:5944407|6.13|-|-|-|-|
|Buffer 2048 Chunk 262144 (77 times)|00:0087335|-0.9|00:0087343|-0.9|00:3599604|3.31|00:5030546|5.03|-|-|-|-|
|Buffer 4096 Chunk 1024 (19532 times)|00:0273495|-0.67|00:0279375|-0.67|05:2997065|62.53|05:1846931|61.15|-|-|-|-|
|Buffer 4096 Chunk 2048 (9766 times)|00:0277645|-0.67|00:0265154|-0.68|04:9375135|58.18|05:1696307|60.97|-|-|-|-|
|Buffer 4096 Chunk 4096 (4883 times)|00:0320191|-0.62|00:0271182|-0.67|05:2375123|61.78|04:8382228|56.99|-|-|-|-|
|Buffer 4096 Chunk 8192 (2442 times)|00:0163789|-0.8|00:0235444|-0.72|02:4381921|28.23|02:2579652|26.07|-|-|-|-|
|Buffer 4096 Chunk 16384 (1221 times)|00:0127659|-0.85|00:0134653|-0.84|01:2535800|14.03|01:4448380|16.32|-|-|-|-|
|Buffer 4096 Chunk 32768 (611 times)|00:0117251|-0.86|00:0121278|-0.85|00:9216701|10.05|01:0658150|11.78|-|-|-|-|
|Buffer 4096 Chunk 65536 (306 times)|00:0097648|-0.88|00:0105241|-0.87|00:5785967|5.94|00:6704591|7.04|-|-|-|-|
|Buffer 4096 Chunk 131072 (153 times)|00:0092556|-0.89|00:0091217|-0.89|00:4179230|4.01|00:6556632|6.86|-|-|-|-|
|Buffer 4096 Chunk 262144 (77 times)|00:0093178|-0.89|00:0088516|-0.89|00:4208976|4.05|00:4461146|4.35|-|-|-|-|
|Buffer 8192 Chunk 1024 (19532 times)|00:0176823|-0.79|00:0172887|-0.79|02:6988024|31.35|02:3845863|27.58|-|-|-|-|
|Buffer 8192 Chunk 2048 (9766 times)|00:0192549|-0.77|00:0178505|-0.79|02:4994286|28.96|02:3852314|27.59|-|-|-|-|
|Buffer 8192 Chunk 4096 (4883 times)|00:0180200|-0.78|00:0173412|-0.79|02:4496074|28.36|02:5128659|29.12|-|-|-|-|
|Buffer 8192 Chunk 8192 (2442 times)|00:0169636|-0.8|00:0196563|-0.76|02:3317699|26.95|02:4688995|28.59|-|-|-|-|
|Buffer 8192 Chunk 16384 (1221 times)|00:0139350|-0.83|00:0125118|-0.85|01:2070165|13.47|01:5011041|16.99|-|-|-|-|
|Buffer 8192 Chunk 32768 (611 times)|00:0113466|-0.86|00:0108140|-0.87|00:9972434|10.95|01:1172985|12.39|-|-|-|-|
|Buffer 8192 Chunk 65536 (306 times)|00:0107974|-0.87|00:0115049|-0.86|00:7030059|7.43|00:7430761|7.91|-|-|-|-|
|Buffer 8192 Chunk 131072 (153 times)|00:0092013|-0.89|00:0091437|-0.89|00:4200519|4.03|00:4941108|4.92|-|-|-|-|
|Buffer 8192 Chunk 262144 (77 times)|00:0092032|-0.89|00:0086074|-0.9|00:3381549|3.05|00:5352180|5.42|-|-|-|-|
|Buffer 16384 Chunk 1024 (19532 times)|00:0141093|-0.83|00:0131557|-0.84|01:5731674|17.86|01:4232958|16.06|-|-|-|-|
|Buffer 16384 Chunk 2048 (9766 times)|00:0127648|-0.85|00:0136260|-0.84|01:5833225|17.98|01:4875603|16.83|-|-|-|-|
|Buffer 16384 Chunk 4096 (4883 times)|00:0136180|-0.84|00:0142399|-0.83|01:4826860|16.77|01:6367318|18.62|-|-|-|-|
|Buffer 16384 Chunk 8192 (2442 times)|00:0140338|-0.83|00:0143935|-0.83|01:5065728|17.06|01:5622857|17.73|-|-|-|-|
|Buffer 16384 Chunk 16384 (1221 times)|00:0135739|-0.84|00:0126288|-0.85|01:4459083|16.33|01:4205029|16.03|-|-|-|-|
|Buffer 16384 Chunk 32768 (611 times)|00:0120210|-0.86|00:0120336|-0.86|00:8208189|8.84|01:2116871|13.52|-|-|-|-|
|Buffer 16384 Chunk 65536 (306 times)|00:0103764|-0.88|00:0111913|-0.87|00:7589705|8.1|00:7021760|7.42|-|-|-|-|
|Buffer 16384 Chunk 131072 (153 times)|00:0104548|-0.87|00:0091834|-0.89|00:5245158|5.29|00:5620197|5.74|-|-|-|-|
|Buffer 16384 Chunk 262144 (77 times)|00:0085181|-0.9|00:0091457|-0.89|00:3400909|3.08|00:4207346|4.04|-|-|-|-|
|Buffer 32768 Chunk 1024 (19532 times)|00:0123755|-0.85|00:0112932|-0.86|01:2094201|13.5|01:0219636|11.25|-|-|-|-|
|Buffer 32768 Chunk 2048 (9766 times)|00:0119889|-0.86|00:0110152|-0.87|00:9538020|10.43|01:0066921|11.07|-|-|-|-|
|Buffer 32768 Chunk 4096 (4883 times)|00:0113963|-0.86|00:0109286|-0.87|01:1288594|12.53|01:1960907|13.34|-|-|-|-|
|Buffer 32768 Chunk 8192 (2442 times)|00:0111720|-0.87|00:0112859|-0.86|01:0129421|11.14|01:3175098|14.79|-|-|-|-|
|Buffer 32768 Chunk 16384 (1221 times)|00:0114591|-0.86|00:0130365|-0.84|00:7974096|8.56|01:1686954|13.01|-|-|-|-|
|Buffer 32768 Chunk 32768 (611 times)|00:0114243|-0.86|00:0110059|-0.87|01:0332774|11.39|01:0851350|12.01|-|-|-|-|
|Buffer 32768 Chunk 65536 (306 times)|00:0099023|-0.88|00:0103605|-0.88|00:6332852|6.59|00:7064896|7.47|-|-|-|-|
|Buffer 32768 Chunk 131072 (153 times)|00:0105679|-0.87|00:0088793|-0.89|00:4527860|4.43|00:5141677|5.16|-|-|-|-|
|Buffer 32768 Chunk 262144 (77 times)|00:0103946|-0.88|00:0084619|-0.9|00:3398193|3.07|00:4939773|4.92|-|-|-|-|
|Buffer 65536 Chunk 1024 (19532 times)|00:0101923|-0.88|00:0108475|-0.87|00:8013945|8.61|00:7967804|8.55|-|-|-|-|
|Buffer 65536 Chunk 2048 (9766 times)|00:0102323|-0.88|00:0102302|-0.88|00:7217738|7.65|01:0586343|11.69|-|-|-|-|
|Buffer 65536 Chunk 4096 (4883 times)|00:0106104|-0.87|00:0110035|-0.87|00:8514405|9.21|00:9357133|10.22|-|-|-|-|
|Buffer 65536 Chunk 8192 (2442 times)|00:0098839|-0.88|00:0103717|-0.88|00:8935249|9.71|00:8981666|9.77|-|-|-|-|
|Buffer 65536 Chunk 16384 (1221 times)|00:0100660|-0.88|00:0101852|-0.88|00:8567987|9.27|00:9377122|10.24|-|-|-|-|
|Buffer 65536 Chunk 32768 (611 times)|00:0102016|-0.88|00:0120747|-0.86|00:6193495|6.42|00:7631151|8.15|-|-|-|-|
|Buffer 65536 Chunk 65536 (306 times)|00:0122890|-0.85|00:0104531|-0.87|00:5989852|6.18|00:5534238|5.63|-|-|-|-|
|Buffer 65536 Chunk 131072 (153 times)|00:0093995|-0.89|00:0112226|-0.87|00:4095094|3.91|00:5113714|5.13|-|-|-|-|
|Buffer 65536 Chunk 262144 (77 times)|00:0090020|-0.89|00:0103322|-0.88|00:3107602|2.72|00:5312821|5.37|-|-|-|-|
|Buffer 131072 Chunk 1024 (19532 times)|00:0100420|-0.88|00:0099661|-0.88|00:5418639|5.5|00:5037630|5.04|-|-|-|-|
|Buffer 131072 Chunk 2048 (9766 times)|00:0106204|-0.87|00:0106465|-0.87|00:6148425|6.37|00:6393039|6.66|-|-|-|-|
|Buffer 131072 Chunk 4096 (4883 times)|00:0100423|-0.88|00:0105020|-0.87|00:5567128|5.67|00:6440819|6.72|-|-|-|-|
|Buffer 131072 Chunk 8192 (2442 times)|00:0098698|-0.88|00:0109821|-0.87|00:4796806|4.75|00:6180529|6.41|-|-|-|-|
|Buffer 131072 Chunk 16384 (1221 times)|00:0099881|-0.88|00:0108562|-0.87|00:4902935|4.88|00:6904365|7.28|-|-|-|-|
|Buffer 131072 Chunk 32768 (611 times)|00:0105228|-0.87|00:0112302|-0.87|00:4743794|4.69|00:5863929|6.03|-|-|-|-|
|Buffer 131072 Chunk 65536 (306 times)|00:0102744|-0.88|00:0114894|-0.86|00:5685208|5.81|00:4981590|4.97|-|-|-|-|
|Buffer 131072 Chunk 131072 (153 times)|00:0094132|-0.89|00:0092101|-0.89|00:4000281|3.79|00:5321374|5.38|-|-|-|-|
|Buffer 131072 Chunk 262144 (77 times)|00:0084105|-0.9|00:0091747|-0.89|00:3623147|3.34|00:4907233|4.88|-|-|-|-|
|Buffer 262144 Chunk 1024 (19532 times)|00:0098533|-0.88|00:0107365|-0.87|00:5065003|5.07|00:4047405|3.85|-|-|-|-|
|Buffer 262144 Chunk 2048 (9766 times)|00:0103612|-0.88|00:0098428|-0.88|00:4536088|4.44|00:5134430|5.15|-|-|-|-|
|Buffer 262144 Chunk 4096 (4883 times)|00:0112320|-0.87|00:0116571|-0.86|00:4321405|4.18|00:5630398|5.75|-|-|-|-|
|Buffer 262144 Chunk 8192 (2442 times)|00:0109314|-0.87|00:0102803|-0.88|00:4201772|4.04|00:5691753|5.82|-|-|-|-|
|Buffer 262144 Chunk 16384 (1221 times)|00:0100361|-0.88|00:0093375|-0.89|00:3967592|3.76|00:5730300|5.87|-|-|-|-|
|Buffer 262144 Chunk 32768 (611 times)|00:0099553|-0.88|00:0112916|-0.86|00:4393231|4.27|00:4809784|4.77|-|-|-|-|
|Buffer 262144 Chunk 65536 (306 times)|00:0101564|-0.88|00:0120477|-0.86|00:6287375|6.54|00:4733327|4.67|-|-|-|-|
|Buffer 262144 Chunk 131072 (153 times)|00:0096847|-0.88|00:0105019|-0.87|00:3367699|3.04|00:5017563|5.01|-|-|-|-|
|Buffer 262144 Chunk 262144 (77 times)|00:0105946|-0.87|00:0090178|-0.89|00:3970450|3.76|00:4388983|4.26|-|-|-|-|


|Read|1|%|2|%|3|%|4|%|5|%|6|%|
|----|----|----|----|----|----|----|----|----|----|----|----|----|
|Buffer 1024 Chunk 1024 (19532 times)|00:0419788|0|00:0431491|0.03|00:0393693|-0.06|00:0400268|-0.05|05:5301129|130.74|04:9392593|116.66|
|Buffer 1024 Chunk 2048 (9766 times)|00:0261928|-0.38|00:0240186|-0.43|00:0233230|-0.44|00:0232803|-0.45|04:3262457|102.06|04:4133262|104.13|
|Buffer 1024 Chunk 4096 (4883 times)|00:0164025|-0.61|00:0168845|-0.6|00:0155539|-0.63|00:0149870|-0.64|04:0503084|95.48|03:9207500|92.4|
|Buffer 1024 Chunk 8192 (2442 times)|00:0110818|-0.74|00:0106082|-0.75|00:0100712|-0.76|00:0100578|-0.76|02:4983265|58.51|02:2928321|53.62|
|Buffer 1024 Chunk 16384 (1221 times)|00:0093968|-0.78|00:0085473|-0.8|00:0077934|-0.81|00:0077604|-0.82|01:3866685|32.03|01:3520346|31.21|
|Buffer 1024 Chunk 32768 (611 times)|00:0070796|-0.83|00:0079791|-0.81|00:0066706|-0.84|00:0065074|-0.84|00:7759915|17.49|00:7296882|16.38|
|Buffer 1024 Chunk 65536 (306 times)|00:0084319|-0.8|00:0076137|-0.82|00:0075628|-0.82|00:0063461|-0.85|00:4704070|10.21|00:4612820|9.99|
|Buffer 1024 Chunk 131072 (153 times)|00:0065710|-0.84|00:0077111|-0.82|00:0060265|-0.86|00:0061087|-0.85|00:3944750|8.4|00:3779465|8|
|Buffer 1024 Chunk 262144 (77 times)|00:0086950|-0.79|00:0071567|-0.83|00:0063303|-0.85|00:0066345|-0.84|00:3000369|6.15|00:2889338|5.88|
|Buffer 2048 Chunk 1024 (19532 times)|00:0267798|-0.36|00:0246013|-0.41|00:0242327|-0.42|00:0242422|-0.42|04:3633221|102.94|04:3818224|103.38|
|Buffer 2048 Chunk 2048 (9766 times)|00:0240826|-0.43|00:0319602|-0.24|00:0232667|-0.45|00:0232821|-0.45|04:4374221|104.71|04:2910820|101.22|
|Buffer 2048 Chunk 4096 (4883 times)|00:0151016|-0.64|00:0160330|-0.62|00:0149930|-0.64|00:0155895|-0.63|04:0947563|96.54|03:8836324|91.51|
|Buffer 2048 Chunk 8192 (2442 times)|00:0103320|-0.75|00:0105093|-0.75|00:0100953|-0.76|00:0100669|-0.76|02:3501585|54.98|02:4483393|57.32|
|Buffer 2048 Chunk 16384 (1221 times)|00:0086246|-0.79|00:0086730|-0.79|00:0078624|-0.81|00:0106783|-0.75|01:3927026|32.18|01:3170450|30.37|
|Buffer 2048 Chunk 32768 (611 times)|00:0090520|-0.78|00:0068118|-0.84|00:0065590|-0.84|00:0068210|-0.84|00:8590734|19.46|00:8000621|18.06|
|Buffer 2048 Chunk 65536 (306 times)|00:0065645|-0.84|00:0077839|-0.81|00:0061152|-0.85|00:0062372|-0.85|00:5074470|11.09|00:4778157|10.38|
|Buffer 2048 Chunk 131072 (153 times)|00:0068227|-0.84|00:0071402|-0.83|00:0061368|-0.85|00:0060262|-0.86|00:3981819|8.49|00:3889244|8.26|
|Buffer 2048 Chunk 262144 (77 times)|00:0064898|-0.85|00:0088364|-0.79|00:0060640|-0.86|00:0060554|-0.86|00:3000240|6.15|00:3224121|6.68|
|Buffer 4096 Chunk 1024 (19532 times)|00:0178517|-0.57|00:0161511|-0.62|00:0159914|-0.62|00:0159537|-0.62|03:8836219|91.51|03:8207007|90.02|
|Buffer 4096 Chunk 2048 (9766 times)|00:0164131|-0.61|00:0191141|-0.54|00:0159336|-0.62|00:0157602|-0.62|03:8391890|90.46|03:7798970|89.04|
|Buffer 4096 Chunk 4096 (4883 times)|00:0152502|-0.64|00:0179392|-0.57|00:0151180|-0.64|00:0151958|-0.64|03:7743324|88.91|03:7558499|88.47|
|Buffer 4096 Chunk 8192 (2442 times)|00:0116687|-0.72|00:0109146|-0.74|00:0101063|-0.76|00:0106633|-0.75|02:3205466|54.28|02:3127801|54.09|
|Buffer 4096 Chunk 16384 (1221 times)|00:0090494|-0.78|00:0084600|-0.8|00:0077930|-0.81|00:0079574|-0.81|01:2296573|28.29|01:3611898|31.43|
|Buffer 4096 Chunk 32768 (611 times)|00:0069326|-0.83|00:0097572|-0.77|00:0065196|-0.84|00:0065084|-0.84|00:7759908|17.49|00:7222950|16.21|
|Buffer 4096 Chunk 65536 (306 times)|00:0071119|-0.83|00:0064823|-0.85|00:0061059|-0.85|00:0060607|-0.86|00:5241162|11.49|00:4796841|10.43|
|Buffer 4096 Chunk 131072 (153 times)|00:0067661|-0.84|00:0066573|-0.84|00:0060475|-0.86|00:0061091|-0.85|00:3796789|8.04|00:3555867|7.47|
|Buffer 4096 Chunk 262144 (77 times)|00:0074660|-0.82|00:0065120|-0.84|00:0066671|-0.84|00:0080043|-0.81|00:2889136|5.88|00:3111353|6.41|
|Buffer 8192 Chunk 1024 (19532 times)|00:0123816|-0.71|00:0116109|-0.72|00:0112411|-0.73|00:0130847|-0.69|02:1631450|50.53|02:2594402|52.82|
|Buffer 8192 Chunk 2048 (9766 times)|00:0116703|-0.72|00:0112890|-0.73|00:0130592|-0.69|00:0123969|-0.7|02:4519360|57.41|02:2650051|52.96|
|Buffer 8192 Chunk 4096 (4883 times)|00:0133518|-0.68|00:0155598|-0.63|00:0108008|-0.74|00:0110034|-0.74|02:4001844|56.18|02:3092218|54.01|
|Buffer 8192 Chunk 8192 (2442 times)|00:0103918|-0.75|00:0110665|-0.74|00:0103910|-0.75|00:0100570|-0.76|02:3851619|55.82|02:3390280|54.72|
|Buffer 8192 Chunk 16384 (1221 times)|00:0081061|-0.81|00:0155065|-0.63|00:0078318|-0.81|00:0107658|-0.74|01:1500797|26.4|01:3037539|30.06|
|Buffer 8192 Chunk 32768 (611 times)|00:0090912|-0.78|00:0092095|-0.78|00:0067736|-0.84|00:0065045|-0.85|00:7799724|17.58|00:7630237|17.18|
|Buffer 8192 Chunk 65536 (306 times)|00:0076789|-0.82|00:0088934|-0.79|00:0060487|-0.86|00:0060730|-0.86|00:5037590|11|00:4667062|10.12|
|Buffer 8192 Chunk 131072 (153 times)|00:0074234|-0.82|00:0086685|-0.79|00:0063264|-0.85|00:0060403|-0.86|00:3370639|7.03|00:3555979|7.47|
|Buffer 8192 Chunk 262144 (77 times)|00:0072217|-0.83|00:0070274|-0.83|00:0060394|-0.86|00:0060738|-0.86|00:3158307|6.52|00:2555770|5.09|
|Buffer 16384 Chunk 1024 (19532 times)|00:0096907|-0.77|00:0105277|-0.75|00:0089439|-0.79|00:0093062|-0.78|01:3778936|31.82|01:3795409|31.86|
|Buffer 16384 Chunk 2048 (9766 times)|00:0099704|-0.76|00:0103757|-0.75|00:0089304|-0.79|00:0086411|-0.79|01:3519546|31.21|01:3093534|30.19|
|Buffer 16384 Chunk 4096 (4883 times)|00:0088177|-0.79|00:0093957|-0.78|00:0085464|-0.8|00:0085835|-0.8|01:4030562|32.42|01:3094188|30.19|
|Buffer 16384 Chunk 8192 (2442 times)|00:0096823|-0.77|00:0103487|-0.75|00:0086035|-0.8|00:0086704|-0.79|01:3964086|32.26|01:4093908|32.57|
|Buffer 16384 Chunk 16384 (1221 times)|00:0092421|-0.78|00:0086504|-0.79|00:0078061|-0.81|00:0077889|-0.81|01:1651490|26.76|01:2630675|29.09|
|Buffer 16384 Chunk 32768 (611 times)|00:0068572|-0.84|00:0077172|-0.82|00:0083411|-0.8|00:0065051|-0.85|00:7278419|16.34|00:7408074|16.65|
|Buffer 16384 Chunk 65536 (306 times)|00:0072116|-0.83|00:0065684|-0.84|00:0063814|-0.85|00:0062769|-0.85|00:5021850|10.96|00:4343907|9.35|
|Buffer 16384 Chunk 131072 (153 times)|00:0065686|-0.84|00:0067487|-0.84|00:0061583|-0.85|00:0062485|-0.85|00:3776952|8|00:3666956|7.74|
|Buffer 16384 Chunk 262144 (77 times)|00:0067916|-0.84|00:0082321|-0.8|00:0060881|-0.85|00:0072777|-0.83|00:2704542|5.44|00:3000264|6.15|
|Buffer 32768 Chunk 1024 (19532 times)|00:0090084|-0.79|00:0079960|-0.81|00:0074413|-0.82|00:0076491|-0.82|00:7167214|16.07|00:7419098|16.67|
|Buffer 32768 Chunk 2048 (9766 times)|00:0082358|-0.8|00:0084466|-0.8|00:0075787|-0.82|00:0077139|-0.82|00:7648870|17.22|00:7482101|16.82|
|Buffer 32768 Chunk 4096 (4883 times)|00:0085036|-0.8|00:0080753|-0.81|00:0072203|-0.83|00:0072912|-0.83|00:7889524|17.79|00:7287297|16.36|
|Buffer 32768 Chunk 8192 (2442 times)|00:0082805|-0.8|00:0077835|-0.81|00:0072449|-0.83|00:0074882|-0.82|00:7556132|17|00:8021335|18.11|
|Buffer 32768 Chunk 16384 (1221 times)|00:0085771|-0.8|00:0089417|-0.79|00:0084092|-0.8|00:0074905|-0.82|00:7315387|16.43|00:7019141|15.72|
|Buffer 32768 Chunk 32768 (611 times)|00:0079243|-0.81|00:0075826|-0.82|00:0065175|-0.84|00:0067289|-0.84|00:8278449|18.72|00:7556202|17|
|Buffer 32768 Chunk 65536 (306 times)|00:0071458|-0.83|00:0082703|-0.8|00:0060597|-0.86|00:0061791|-0.85|00:4869641|10.6|00:4444787|9.59|
|Buffer 32768 Chunk 131072 (153 times)|00:0076543|-0.82|00:0065801|-0.84|00:0060655|-0.86|00:0061360|-0.85|00:3666935|7.74|00:3444788|7.21|
|Buffer 32768 Chunk 262144 (77 times)|00:0070924|-0.83|00:0067201|-0.84|00:0061039|-0.85|00:0062903|-0.85|00:2777989|5.62|00:3111371|6.41|
|Buffer 65536 Chunk 1024 (19532 times)|00:0077747|-0.81|00:0086479|-0.79|00:0071338|-0.83|00:0070015|-0.83|00:5148330|11.26|00:4265353|9.16|
|Buffer 65536 Chunk 2048 (9766 times)|00:0099621|-0.76|00:0084663|-0.8|00:0070376|-0.83|00:0075519|-0.82|00:4741115|10.29|00:5333724|11.71|
|Buffer 65536 Chunk 4096 (4883 times)|00:0074781|-0.82|00:0071508|-0.83|00:0067835|-0.84|00:0066705|-0.84|00:4833818|10.51|00:4593067|9.94|
|Buffer 65536 Chunk 8192 (2442 times)|00:0071713|-0.83|00:0095553|-0.77|00:0066372|-0.84|00:0067115|-0.84|00:4865353|10.59|00:4574454|9.9|
|Buffer 65536 Chunk 16384 (1221 times)|00:0068595|-0.84|00:0079855|-0.81|00:0095972|-0.77|00:0068520|-0.84|00:4778197|10.38|00:4407557|9.5|
|Buffer 65536 Chunk 32768 (611 times)|00:0076130|-0.82|00:0089051|-0.79|00:0083970|-0.8|00:0072652|-0.83|00:5241259|11.49|00:4796701|10.43|
|Buffer 65536 Chunk 65536 (306 times)|00:0070351|-0.83|00:0069758|-0.83|00:0070668|-0.83|00:0060915|-0.85|00:5093024|11.13|00:4572504|9.89|
|Buffer 65536 Chunk 131072 (153 times)|00:0070399|-0.83|00:0079535|-0.81|00:0066363|-0.84|00:0060190|-0.86|00:3167137|6.54|00:4000354|8.53|
|Buffer 65536 Chunk 262144 (77 times)|00:0064551|-0.85|00:0077720|-0.81|00:0073232|-0.83|00:0060631|-0.86|00:2926161|5.97|00:2333692|4.56|
|Buffer 131072 Chunk 1024 (19532 times)|00:0087841|-0.79|00:0076258|-0.82|00:0083323|-0.8|00:0074612|-0.82|00:3222468|6.68|00:3333427|6.94|
|Buffer 131072 Chunk 2048 (9766 times)|00:0076939|-0.82|00:0074651|-0.82|00:0069421|-0.83|00:0070777|-0.83|00:3222935|6.68|00:3222607|6.68|
|Buffer 131072 Chunk 4096 (4883 times)|00:0074925|-0.82|00:0093939|-0.78|00:0067270|-0.84|00:0098198|-0.77|00:3611596|7.6|00:3225547|6.68|
|Buffer 131072 Chunk 8192 (2442 times)|00:0075701|-0.82|00:0076844|-0.82|00:0067733|-0.84|00:0065688|-0.84|00:3444706|7.21|00:3444689|7.21|
|Buffer 131072 Chunk 16384 (1221 times)|00:0076139|-0.82|00:0079868|-0.81|00:0095335|-0.77|00:0072296|-0.83|00:3963311|8.44|00:3500727|7.34|
|Buffer 131072 Chunk 32768 (611 times)|00:0086653|-0.79|00:0095956|-0.77|00:0082763|-0.8|00:0090952|-0.78|00:3724173|7.87|00:3717806|7.86|
|Buffer 131072 Chunk 65536 (306 times)|00:0092424|-0.78|00:0104146|-0.75|00:0072514|-0.83|00:0090511|-0.78|00:3747487|7.93|00:3444733|7.21|
|Buffer 131072 Chunk 131072 (153 times)|00:0065976|-0.84|00:0065275|-0.84|00:0062706|-0.85|00:0060767|-0.86|00:3642082|7.68|00:3463350|7.25|
|Buffer 131072 Chunk 262144 (77 times)|00:0074152|-0.82|00:0070446|-0.83|00:0068901|-0.84|00:0061105|-0.85|00:2889100|5.88|00:2778022|5.62|
|Buffer 262144 Chunk 1024 (19532 times)|00:0080658|-0.81|00:0085477|-0.8|00:0071375|-0.83|00:0071783|-0.83|00:3111354|6.41|00:3111387|6.41|
|Buffer 262144 Chunk 2048 (9766 times)|00:0083434|-0.8|00:0086162|-0.79|00:0069485|-0.83|00:0069917|-0.83|00:3037426|6.24|00:2666904|5.35|
|Buffer 262144 Chunk 4096 (4883 times)|00:0082709|-0.8|00:0077682|-0.81|00:0069155|-0.84|00:0081391|-0.81|00:2778368|5.62|00:3000234|6.15|
|Buffer 262144 Chunk 8192 (2442 times)|00:0080895|-0.81|00:0079177|-0.81|00:0070534|-0.83|00:0068130|-0.84|00:3000248|6.15|00:3000257|6.15|
|Buffer 262144 Chunk 16384 (1221 times)|00:0103203|-0.75|00:0077917|-0.81|00:0079654|-0.81|00:0082295|-0.8|00:3047126|6.26|00:3000238|6.15|
|Buffer 262144 Chunk 32768 (611 times)|00:0081354|-0.81|00:0091889|-0.78|00:0084937|-0.8|00:0088537|-0.79|00:3333743|6.94|00:2778011|5.62|
|Buffer 262144 Chunk 65536 (306 times)|00:0084484|-0.8|00:0087837|-0.79|00:0075419|-0.82|00:0075529|-0.82|00:2815280|5.71|00:2666869|5.35|
|Buffer 262144 Chunk 131072 (153 times)|00:0083562|-0.8|00:0090151|-0.79|00:0090435|-0.78|00:0078818|-0.81|00:3000244|6.15|00:2592945|5.18|
|Buffer 262144 Chunk 262144 (77 times)|00:0069125|-0.84|00:0075942|-0.82|00:0078201|-0.81|00:0062875|-0.85|00:2889138|5.88|00:3000225|6.15|

## File generation V1
The logic to generate the rows for the measurements isn't too complicated but writing the file might take a lot of time. So before generating the final measurements file with 1B rows (~12GB) it would be best to improve the code first and test it with a smaller amount of rows.

The first version uses a simple StreamWriter which writes one random line at a time.
```csharp
using (var writer = new StreamWriter(filePath, false, Encoding.UTF8)) {
writer.NewLine = "\n";

	for (var i = 0; i < rowCount; i++) {
		var line = GetLine(names);
		writer.WriteLine(line);
	}
}
```
This will be the base line with **10M rows (~550MB)** for further improvements.

|                                             | Duration  |(new-old)/old*100% | Commit |
|---------------------------------------------|-----------|-------------------|--------|
| StreamWriter.WriteLine(line as string)      | 00:30:288 | base line	      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/f20bdce347f4ec549f1cc0eeb20785c9807db7c1/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) |
| FileStream.Write(line as byte array)        | 00:23:201 |      -23,40%      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/a8993ae4264db8d9e07f05d7dec939078dd51183/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) |
| FileStream.Write + 10 lines as byte array   | 00:21:569 |      -28,79%      ||
| FileStream.Write + 100 lines as byte array  | 00:18:716 |      -38,21%      ||
| FileStream.Write + 1k lines as byte array   | 00:16:937 |      -44,08%      | [Link](https://github.com/thoeltig/CodingChallenge.1BRC/blob/c18ad89905c30196cdde95f5c5f4e0f97c86e32e/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs)|
| FileStream.Write + 2k lines as byte array   | 00:17:426 |      -42,26%      ||
| FileStream.Write + 2.5k lines as byte array | 00:18:244 |      -39,76%      ||

From the results it is clear that writing a byte array of multiple lines to the FileStream has the best performance. 1000 lines with random line length seems to have a slightly faster performance but might only be on my device and the optimal number will vary from one device to the next depending on the IO bottleneck and the used buffer size.
- The buffer size of the FileStream can also be adjusted. The default is 4096 and increasing it might help. 
- FileStream doesn't support parallel file writes but the random names and final lines can be generated in parallel.

After a bit of [refactoring](https://github.com/thoeltig/CodingChallenge.1BRC/blob/2ba8b8ee1a633fadb83977bb10ce99e2bd691250/src/1BRC.ConsoleRunner/MeasurementsGenerator.cs) I was ready to run a test with a couple of combinations:

| Buffer size + line count					  | Duration  |(new-old)/old*100% |
|---------------------------------------------|-----------|-------------------|
| 4096  + 500 								  | 00:16:416 | new base line  	  |
| 8192  + 500 								  | 00:16:272 |       -0,88%      |
| 16384 + 500  								  | 00:16:393 |       -0,14%      |
| 32768 + 500  								  | 00:16:016 |       -2,44%      |
| 65536 + 500  								  | 00:15:910 |       -3,08%      |
| 4096  + 1000 								  | 00:15:147 |       -7,73%      |
| 8192  + 1000 								  | 00:14:997 |       -8,64%      |
| 16384 + 1000 								  | 00:15:019 |       -8,51%      |
| 32768 + 1000 								  | 00:15:204 |       -7,38%      |
| 65536 + 1000 								  | 00:15:001 |       -8,62%      |
| 4096  + 2000 								  | 00:14:444 |      -12,01%      |
| 8192  + 2000 								  | 00:14:523 |      -11,53%      |
| 16384 + 2000 								  | 00:14:456 |      -11,94%      |
| 32768 + 2000 								  | 00:14:493 |      -11,71%      |
| 65536 + 2000 								  | 00:14:347 |      -12,60%      |
| 4096  + 4000  							  | 00:14:011 |      -14,65%      |
| 8192  + 4000  							  | 00:13:696 |      -16,57%      |
| 16384 + 4000  							  | 00:13:659 |      -16,79%      |
| **32768 + 4000**  						  |**00:13:616**|  **-17,06%**    |
| 65536 + 4000  							  | 00:13:862 |      -15,56%      |
| 4096  + 5000  							  | 00:14:084 |      -14,21%      |
| 8192  + 5000  							  | 00:13:848 |      -15,64%      |
| 16384 + 5000  							  | 00:13:825 |      -15,78%      |
| 32768 + 5000  							  | 00:14:133 |      -13,91%      |
| 65536 + 5000  							  | 00:13:906 |      -15,29%      |
| 4096  + 8000  							  | 00:14:107 |      -14,06%      |
| 8192  + 8000  							  | 00:14:133 |      -13,91%      |
| 16384 + 8000  							  | 00:14:148 |      -13,82%      |
| 32768 + 8000  							  | 00:14:064 |      -14,33%      |
| 65536 + 8000  							  | 00:14:213 |      -13,42%      |
| 4096  + 10000  							  | 00:14:805 |       -9,81%      |
| 8192  + 10000  							  | 00:14:855 |       -9,51%      |
| 16384 + 10000  							  | 00:14:991 |       -8,68%      |
| 32768 + 10000  							  | 00:14:906 |       -9,20%      |
| 65536 + 10000  							  | 00:14:901 |       -9,23%      |

Test result:
- First of all I noticed that I screwed up the time measurements in the test before because the file delete was inside the time tracking code. This is not a big adjustment but it changed the measured time slightly and that is why the whole default buffer sizes needs to be tested again with the different line count.
- This took a while to write down but now it is clear that generating 4000 lines in parallel and writing them to file with a file buffer size of 32768 has the best performance (these values will differ depending on the hardware).
- The problem with this result is that it is only an approximation to the correct combination because the lines have a random sizes from 3 to 106 bytes which will result in 12000 to 424000 bytes written to the file with a buffer of 32768. This is not really optimal but the best result which can be archieved with this version.


**It took 23:13:214 to generate the final measurements file with 1B rows.**