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

## Writing & reading the measurements file

### Writing
There are a couple of classes & methods that could be used to write the data to a file:
- _File.CreateText_ & _FileInfo.CreateText_ will both create a file and return a _StreamWriter_ to write to it.
- _File.Create_ will also create a file and return a _FileWriter_ to write to it.
	- The stream buffer size can be overwritten (default 4096).
- Alternatively _StreamWriter_ & _FileStream_ can be created directly to write to the file.
	- _StreamWriter_ supports writing a string, a char array or a byte array.
	- _FileStream_ supports only a byte array.
	- Both allow an overwrite of the default stream buffer size of 4096.

The test case will be writing 10000000 bytes to a file:
- as a single string
- as a char array
- as a byte array
- the privious three in multiple blocks of size
	- 4096
	- 8192
	- 16384
	- 32768
	- 65536
- also if possible the buffer size is set for each test with buffer sizes
	- 4096
	- 8192
	- 16384
	- 32768
	- 65536
- the elapsed time is the average of 12 tests as the fractional portion of a second

Abbreviations:
- File.CT = File.CreateText
- File.C = File.Create
- SW = StreamWriter
- FS = FileStream

|											|string		|%			|char array	|%		|
|-------------------------------------------|-----------|-----------|-----------|-------|
|File.CT + SW + 1 Write						|1293107	|base line	|1293548	|0.0%	|
|File.CT + SW + 2442 Writes (Block 4096)	|1221823	|0.0%		|1121599	|0.0%	|
|File.CT + SW + 1221 Writes (Block 8192)	|1202446	|0.0%		|1114435	|0.0%	|
|File.CT + SW + 611 Writes (Block 16384)	|1250209	|0.0%		|1178940	|0.0%	|
|File.CT + SW + 306 Writes (Block 32768)	|1256339	|0.0%		|1153100	|0.0%	|
|File.CT + SW + 153 Writes (Block 65536)	|1217394	|0.0%		|1170953	|0.0%	|
|FileInfo + SW + 1 Write					|1219227	|0.0%		|1182180	|0.0%	|
|FileInfo + SW + 2442 Writes (Block 4096)	|1320695	|0.0%		|1116693	|0.0%	|
|FileInfo + SW + 1221 Writes (Block 8192)	|1284690	|0.0%		|1158506	|0.0%	|
|FileInfo + SW + 611 Writes (Block 16384)	|1238602	|0.0%		|1124409	|0.0%	|
|FileInfo + SW + 306 Writes (Block 32768)	|1225385	|0.0%		|1127841	|0.0%	|
|FileInfo + SW + 153 Writes (Block 65536)	|1205389	|0.0%		|1200050	|0.0%	|
|SW + Buffer 4096 +1 Write					|1229701	|0.0%		|1236782	|0.0%	|
|SW + Buffer 4096 + 2442 Writes (Block 4096)|1224911	|0.0%		|1123864	|0.0%	|
|SW + Buffer 4096 + 1221 Writes (Block 8192)|1246995	|0.0%		|1111940	|0.0%	|
|SW + Buffer 4096 + 611 Writes (Block 16384)|1225226	|0.0%		|1136849	|0.0%	|
|SW + Buffer 4096 + 306 Writes (Block 32768)|1334478	|0.0%		|1185733	|0.0%	|
|SW + Buffer 4096 + 153 Writes (Block 65536)|1289893	|0.0%		|1329354	|0.0%	|
**INFO: StreamWriter only used with default buffer size of 4096 in all tests**

|														|byte array	|%		|
|-------------------------------------------------------|-----------|-------|
|File.C + FS (Buffer 4096) + 2442 Writes (Block 4096) 	|0231060	|0.0%	|
|File.C + FS (Buffer 4096) + 1221 Writes (Block 8192) 	|0114511	|0.0%	|
|File.C + FS (Buffer 4096) + 611 Writes (Block 16384) 	|0078872	|0.0%	|
|File.C + FS (Buffer 4096) + 306 Writes (Block 32768) 	|0073255	|0.0%	|
|File.C + FS (Buffer 4096) + 153 Writes (Block 65536) 	|0065392	|0.0%	|
|File.C + FS (Buffer 8192) + 2442 Writes (Block 4096) 	|0116835	|0.0%	|
|File.C + FS (Buffer 8192) + 1221 Writes (Block 8192) 	|0102979	|0.0%	|
|File.C + FS (Buffer 8192) + 611 Writes (Block 16384) 	|0072384	|0.0%	|
|File.C + FS (Buffer 8192) + 306 Writes (Block 32768) 	|0069706	|0.0%	|
|File.C + FS (Buffer 8192) + 153 Writes (Block 65536) 	|0058480	|0.0%	|
|File.C + FS (Buffer 16384) + 2442 Writes (Block 4096)	|0078453	|0.0%	|
|File.C + FS (Buffer 16384) + 1221 Writes (Block 8192)	|0076202	|0.0%	|
|File.C + FS (Buffer 16384) + 611 Writes (Block 16384)	|0073298	|0.0%	|
|File.C + FS (Buffer 16384) + 306 Writes (Block 32768)	|0071450	|0.0%	|
|File.C + FS (Buffer 16384) + 153 Writes (Block 65536)	|0058735	|0.0%	|
|File.C + FS (Buffer 32768) + 2442 Writes (Block 4096)	|0064887	|0.0%	|
|File.C + FS (Buffer 32768) + 1221 Writes (Block 8192)	|0063793	|0.0%	|
|File.C + FS (Buffer 32768) + 611 Writes (Block 16384)	|0062089	|0.0%	|
|File.C + FS (Buffer 32768) + 306 Writes (Block 32768)	|0066671	|0.0%	|
|File.C + FS (Buffer 32768) + 153 Writes (Block 65536)	|0052066	|0.0%	|
|File.C + FS (Buffer 65536) + 2442 Writes (Block 4096)	|0057004	|0.0%	|
|File.C + FS (Buffer 65536) + 1221 Writes (Block 8192)	|0054716	|0.0%	|
|File.C + FS (Buffer 65536) + 611 Writes (Block 16384)	|0056519	|0.0%	|
|File.C + FS (Buffer 65536) + 306 Writes (Block 32768)	|0065918	|0.0%	|
|File.C + FS (Buffer 65536) + 153 Writes (Block 65536)	|0052638	|0.0%	|
|Average of the above									|-			|-		|
|FS (Buffer 4096) + 2442 Writes (Block 4096) 			|0217797	|0.0%	|
|FS (Buffer 4096) + 1221 Writes (Block 8192) 			|0125412	|0.0%	|
|FS (Buffer 4096) + 611 Writes (Block 16384) 			|0077692	|0.0%	|
|FS (Buffer 4096) + 306 Writes (Block 32768) 			|0061831	|0.0%	|
|FS (Buffer 4096) + 153 Writes (Block 65536) 			|0054321	|0.0%	|
|FS (Buffer 8192) + 2442 Writes (Block 4096) 			|0118598	|0.0%	|
|FS (Buffer 8192) + 1221 Writes (Block 8192) 			|0106295	|0.0%	|
|FS (Buffer 8192) + 611 Writes (Block 16384) 			|0074605	|0.0%	|
|FS (Buffer 8192) + 306 Writes (Block 32768) 			|0059227	|0.0%	|
|FS (Buffer 8192) + 153 Writes (Block 65536) 			|0054556	|0.0%	|
|FS (Buffer 16384) + 2442 Writes (Block 4096)			|0078896	|0.0%	|
|FS (Buffer 16384) + 1221 Writes (Block 8192)			|0076517	|0.0%	|
|FS (Buffer 16384) + 611 Writes (Block 16384)			|0069817	|0.0%	|
|FS (Buffer 16384) + 306 Writes (Block 32768)			|0070845	|0.0%	|
|FS (Buffer 16384) + 153 Writes (Block 65536)			|0051233	|0.0%	|
|FS (Buffer 32768) + 2442 Writes (Block 4096)			|0064147	|0.0%	|
|FS (Buffer 32768) + 1221 Writes (Block 8192)			|0060164	|0.0%	|
|FS (Buffer 32768) + 611 Writes (Block 16384)			|0063628	|0.0%	|
|FS (Buffer 32768) + 306 Writes (Block 32768)			|0067186	|0.0%	|
|FS (Buffer 32768) + 153 Writes (Block 65536)			|0054407	|0.0%	|
|FS (Buffer 65536) + 2442 Writes (Block 4096)			|0056377	|0.0%	|
|FS (Buffer 65536) + 1221 Writes (Block 8192)			|0055779	|0.0%	|
|FS (Buffer 65536) + 611 Writes (Block 16384)			|0053956	|0.0%	|
|FS (Buffer 65536) + 306 Writes (Block 32768)			|0154740	|0.0%	|
|FS (Buffer 65536) + 153 Writes (Block 65536)			|0064157	|0.0%	|
|Average of the above									|-			|0.0%	|

FileStream is clearly faster but finding the correct buffer & block size combinations seems rather tricky.

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