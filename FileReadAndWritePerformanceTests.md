# Writing & reading a file
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

## Setup
Old notebook
- Intel Core i5-4200U - 2.3 GHz
- DDR3 - 8GB RAM - 1600 MHz
- Toshiba MQ01ABF050 - Read 100 MB/s Write 96 MB/s


## First test: Comparison of the different classes 
The test will write 10 MB to a file:
- Inputs
	- a single string
	- a char array
	- a byte array
- Block sizes (and buffer size for _FileStream_)
	- 4096
	- 8192
	- 16384
	- 32768
	- 65536
- The elapsed time is the average of 12 tests as the fractional portion of a second.
- Abbreviations:
	- File.CT = File.CreateText
	- File.C = File.Create
	- SW = StreamWriter
	- FS = FileStream
- [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/first_test_classcomparison/Write_NetFramework.md)

<ins>**_StreamWriter_:**</ins>

|Position|Class|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
|----|-----|-----|-----|----|-----|----|----|
|Base line|File.CT + string|1024|1293107|1|_FileOptions.SequentialScan_|0.00|00:1293548|
|1.|SW + char array|4096|1221|8192|_FileOptions.None_|-14.01|00:1111940|
|2.|File.CT + char array|1024|1221|8192|_FileOptions.None_|-13.82|00:1114435|
|3.|FileInfo + char array|32768|2442|4096|_FileOptions.SequentialScan_|-13.64|00:1116693|


> [!NOTE]
> _StreamWriter_ was only tested with the default buffer size of 4096 after comparing it with the _FileStream_ default.


<ins>**_FileStream_:**</ins>

|Position|Class|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
|----|-----|-----|-----|----|-----|----|----|
|Base line|File.C|4096|2442|4096|_FileOptions.None_|-82.13|00:0231060|
|1.|FS|16384|153|65536|_FileOptions.None_|-96.04|00:0051233|
|2.|File.C|32768|153|65536|_FileOptions.None_|-95.97|00:0052066|
|3.|File.C|65536|153|65536|_FileOptions.None_|-95.93|00:0052638|


> [!Note]
> _FileStream_ is clearly faster but finding the correct buffer & block size combinations seems rather tricky.


<ins>**Simple explanation of the internal logic of a _FileStream_:**</ins>
- Has an internal buffer which holds the bytes. If the buffer is full it is written to the file system cache.
	- This can be forced by calling _FileStream.Flush_. This is also called when disposing the _FileStream_.
- The file system writes the data lazily to disk depending on the hard disk write speed.
- Reading the bytes is similiar. Depending on the access type more or less is cached by the file systen which in turn allows for faster sequential paging through the data or faster access at random positions.
- Read & write have a couple of options which can be added when calling the methods. For this case the following are interesting:
	- _FileOptions.None_ will pass no additional access flags to the file system. So it will try to guess the optimal cache size depending on the access pattern.
	- _FileOptions.RandomAccess_ will cache less because it expects the file to be accessed at random positions by multiple applications.
	- _FileOptions.SequentialScan_ will cache more because it expects the file to be accessed sequentially by a single application.
	- _FileOptions.WriteThrough_ will write to file system cache but is flushed to disk without delay.
	- _FILE_FLAG_NO_BUFFERING_ will ignore the file system cache but has has special memory alignment requierements.
		- It isn't included in _FileOptions_ but can still be passed down with 0x20000000.
		- File, buffer & block size need to be an integer multiple of 512 bytes.	
	- If _FILE_FLAG_NO_BUFFERING_ is used in combination with _FileOptions.WriteThrough_ the file system cache is ignored and the data is immediately flushed to disk.
	
	
## Second test: FileStream, buffer and block size
The test will use the _FileStream_ to write & read 30 MB to & from a file:
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
	5. _FILE_FLAG_NO_BUFFERING_ 
	6. _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_ 
	7. _FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_
- The elapsed time is the median of 12 tests as the fractional portion of a second.
	- The elapsed times of each test are sorted, first and last quarter is ignored to avoid using the extrem values and then the average is calcualted from the remaining half. 
- Executed code
	- .NET Framework 4.7.2 Console with byte[]
	- .NET 5 (Core) Console with Spans<byte>
	
	
### Write
- .NET Framework 4.7.2 + byte array
	- Access types 1. - 4. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Write_NetFramework.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:1299553|
	|1.|1024|262144|115|_FileOptions.SequentialScan_|-90.62|00:0121946|
	|2.|131072|262144|115|_FileOptions.SequentialScan_|-90.55|00:0122786|
	|3.|32768|262144|115|_FileOptions.SequentialScan_|-90.49|00:0123639|
	|4.|8192|262144|115|_FileOptions.None_|-90.43|00:0124345|
	|5.|16384|262144|115|_FileOptions.SequentialScan_|-90.31|00:0125923|
	|6.|65536|262144|115|_FileOptions.None_|-90.29|00:0126154|
	|7.|2048|262144|115|_FileOptions.SequentialScan_|-90.24|00:0126871|
	|8.|131072|262144|115|_FileOptions.None_|-90.11|00:0128578|
	|9.|65536|262144|115|_FileOptions.SequentialScan_|-90.07|00:0129060|
	|10.|4096|262144|115|_FileOptions.None_|-89.86|00:0131826|
	
	- Access types 5. - 7. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Write_NetFramework_NoBuffer.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FILE_FLAG_NO_BUFFERING_||14:0765745|
	|1.|32768|262144|115|_FILE_FLAG_NO_BUFFERING_|-95.46|00:6384932|
	|2.|8192|262144|115|_FILE_FLAG_NO_BUFFERING_|-95.46|00:6393680|
	|3.|262144|262144|115|_FILE_FLAG_NO_BUFFERING_|-95.35|00:6548806|
	|4.|1024|262144|115|_FILE_FLAG_NO_BUFFERING_|-95.16|00:6818550|
	|5.|65536|262144|115|_FILE_FLAG_NO_BUFFERING_|-95.04|00:6987442|
	|6.|131072|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.01|00:7022802|
	|7.|4096|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.99|00:7058127|
	|8.|2048|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.95|00:7113264|
	|9.|16384|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.83|00:7273666|
	|10.|262144|8192|3663|_FILE_FLAG_NO_BUFFERING_|-94.79|00:7328590|
	
- .NET 5 (Core) + byte spans 
	- Access types 1. - 4. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Write_NetCore.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:1514422|
	|1.|4096|262144|115|_FileOptions.None_|-91.69|00:0125792|
	|2.|32768|262144|115|_FileOptions.SequentialScan_|-91.69|00:0125872|
	|3.|262144|262144|115|_FileOptions.SequentialScan_|-91.61|00:0127124|
	|4.|8192|262144|115|_FileOptions.SequentialScan_|-91.51|00:0128543|
	|5.|4096|262144|115|_FileOptions.SequentialScan_|-91.46|00:0129328|
	|6.|1024|262144|115|_FileOptions.SequentialScan_|-91.39|00:0130422|
	|7.|32768|262144|115|_FileOptions.None_|-91.36|00:0130781|
	|8.|16384|262144|115|_FileOptions.SequentialScan_|-91.12|00:0134531|
	|9.|65536|262144|115|_FileOptions.SequentialScan_|-91.10|00:0134734|
	|10.|16384|262144|115|_FileOptions.None_|-91.09|00:0134885|
	
	- Access types 5. - 7. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Write_NetCore_NoBuffer.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FILE_FLAG_NO_BUFFERING_||08:5941065|
	|1.|262144|8192|3663|_FILE_FLAG_NO_BUFFERING_|-93.98|00:5170241|
	|2.|8192|262144|115|_FILE_FLAG_NO_BUFFERING_|-93.97|00:5183650|
	|3.|131072|262144|115|_FILE_FLAG_NO_BUFFERING_|-93.82|00:5312722|
	|4.|2048|262144|115|_FILE_FLAG_NO_BUFFERING_|-93.82|00:5315240|
	|5.|131072|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-93.76|00:5358676|
	|6.|262144|32768|916|_FILE_FLAG_NO_BUFFERING_|-93.71|00:5401459|
	|7.|262144|2048|14649|_FILE_FLAG_NO_BUFFERING_|-93.69|00:5421775|
	|8.|16384|262144|115|_FILE_FLAG_NO_BUFFERING_|-93.67|00:5440487|
	|9.|262144|262144|115|_FILE_FLAG_NO_BUFFERING_|-93.67|00:5443361|
	|10.|262144|65536|458|_FILE_FLAG_NO_BUFFERING_|-93.62|00:5487314|

- Conclusion
	- For access type 1. - 4. both .NET versions had the best results with block size 262144 & _FileOptions.SequentialScan_ but the buffer sizes is random. There seems to be no major difference in speed between using byte arrays & spans of bytes when writing the data to the stream but in total the .NET Framework version ran slightly faster looking at the actual execution times.
		- The benefit of spans is less memory allocation, faster access & modification of the underlying memory. A difference between byte array & spans might show later in the finished code.
		- Need to test larger block & chunk sizes.
		- For the currently used versions access type 3. & 4. can be ignored for future tests. Might have better results in newer .NET versions.
	- For access type 5. - 7. both .NET versions had the best results with block size 262144 & _FILE_FLAG_NO_BUFFERING but the buffer sizes is random.
		- .NET 5 performed better than .NET Framework but in comparison with 1. & 2. access type they were really slow.
		- For the currently used versions access type 5. - 7. can be ignored for future tests. Might have better results in newer .NET versions.

### Read	
- .NET Framework 4.7.2 + byte array
	- Access types 1. - 4. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Read_NetFramework.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:0693528|
	|1.|8192|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.30|00:0088072|
	|2.|1024|131072|229|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.22|00:0088628|
	|3.|131072|131072|229|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.18|00:0088898|
	|4.|16384|262144|115|_FileOptions.WriteThrough_|-87.16|00:0089060|
	|5.|131072|131072|229|_FileOptions.WriteThrough_|-87.14|00:0089171|
	|6.|1024|262144|115|_FileOptions.WriteThrough_|-87.14|00:0089188|
	|7.|65536|262144|115|_FileOptions.WriteThrough_|-87.14|00:0089192|
	|8.|32768|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.12|00:0089313|
	|9.|2048|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.12|00:0089314|
	|10.|32768|262144|115|_FileOptions.WriteThrough_|-87.12|00:0089352|
	
	- Access types 5. - 7. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Read_NetFramework_NoBuffer.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FILE_FLAG_NO_BUFFERING_||07:4466310|
	|1.|8192|262144|115| _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.22|00:3555872|
	|2.|65536|262144|115|_FILE_FLAG_NO_BUFFERING_|-95.08|00:3666949|
	|3.|262144|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.08|00:3666935|
	|4.|262144|131072|229|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.08|00:3667076|
	|5.|65536|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.08|00:3667002|
	|6.|262144|32768|916|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.93|00:3778057|
	|7.|1024|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.93|00:3778061|
	|8.|16384|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.93|00:3778302|
	|9.|131072|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.78|00:3889167|
	|10.|2048|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.78|00:3889174|
	
- .NET 5 (Core) + byte spans 	
	- Access types 1. - 4. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Read_NetCore.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:0704307|
	|1.|2048|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-88.12|00:0083699|
	|2.|1024|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.61|00:0087258|
	|3.|1024|262144|115|_FileOptions.WriteThrough_|-87.52|00:0087911|
	|4.|2048|262144|115|_FileOptions.WriteThrough_|-87.48|00:0088184|
	|5.|65536|131072|229|_FileOptions.WriteThrough_|-87.48|00:0088207|
	|6.|4096|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.47|00:0088221|
	|7.|32768|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.45|00:0088392|
	|8.|32768|131072|229|_FileOptions.WriteThrough_|-87.44|00:0088446|
	|9.|1024|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.43|00:0088549|
	|10.|2048|131072|229|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.41|00:0088656|
	
	- Access types 5. - 7. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/second_test_filestream/Read_NetCore_NoBuffer.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FILE_FLAG_NO_BUFFERING_||07:4339171|
	|1.|8192|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.17|00:3593126|
	|2.|16384|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-95.07|00:3667849|
	|3.|131072|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.92|00:3777291|
	|4.|65536|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.92|00:3778078|
	|5.|262144|262144|115|_FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.77|00:3889190|
	|6.|8192|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.77|00:3889193|
	|7.|16384|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.77|00:3889197|
	|8.|262144|131072|229|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_|-94.77|00:3889210|
	|9.|1024|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.77|00:3889253|
	|10.|262144|262144|115|_FILE_FLAG_NO_BUFFERING_|-94.77|00:3889264|
	
- Conclusion
	- For access type 1. - 4. both .NET versions had the best results with block size 262144 or 131072 & _FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ or _FileOptions.WriteThrough_ but the buffer sizes is random. There seems to be no major difference in speed between using byte arrays & spans of bytes when reading the data from the stream but in total the .NET 5 version ran slightly faster looking at the actual execution times.
		- The benefit of spans is less memory allocation, faster access & modification of the underlying memory. A difference between byte array & spans might show later in the finished code.
		- Need to test larger block & chunk sizes.
		- For the currently used versions access type 1. & 2. can be ignored for future tests. Might have better results in newer .NET versions.
	- For access type 5. - 7. both .NET versions had the best results with block size 262144 & _FileOptions.SequentialScan_ + _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_ for .NET Framework or _FileOptions.WriteThrough_ + _FILE_FLAG_NO_BUFFERING_ for .NET 5 but the buffer sizes is random.
		- Both versions performed similiar but in comparison with 3. & 4. access type they were really slow.
		- For the currently used versions access type 5. - 7. can be ignored for future tests. Might have better results in newer .NET versions.


## Third test: FileStream, bigger buffer and block size
The test will use the _FileStream_ to write & read 30 MB to & from a file:
- Buffer sizes
	- 1024
	- 2048
	- 4096
	- 8192
	- 16384
	- 32768
	- 65536
	- 131072
	- 262144
	- 524288
	- 1048576
	- 2097152
	- 4194304
- Block sizes
	- 131072
	- 262144
	- 524288
	- 1048576
	- 2097152
	- 4194304
- Access types
	- write
		1. _FileOptions.None_
		2. _FileOptions.SequentialScan_
	- Read
		1. _FileOptions.None_
		2. _FileOptions.SequentialScan_
		3. _FileOptions.WriteThrough_
		4. _FileOptions.SequentialScan_ + _FileOptions.WriteThrough_
- The elapsed time is the median of 12 tests as the fractional portion of a second.
	- The elapsed times of each test are sorted, first and last quarter is ignored to avoid using the extrem values and then the average is calcualted from the remaining half. 
- Executed code
	- .NET Framework 4.7.2 Console with byte[]
	- .NET 5 (Core) Console with Spans<byte>
	
	
### Write
- .NET Framework 4.7.2 + byte array + Access types 1. + 2. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/third_test_filestream_biggerblocksize/Write_NetFramework.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:1299553|
	|1.|262144|2097152|15|_FileOptions.SequentialScan_|-90.91|00:0118064|
	|2.|2048|4194304|8|_FileOptions.None_|-90.87|00:0118581|
	|3.|8192|4194304|8|_FileOptions.None_|-90.87|00:0118671|
	|4.|1024|2097152|15|_FileOptions.None_|-90.82|00:0119351|
	|5.|4096|4194304|8|_FileOptions.SequentialScan_|-90.79|00:0119689|
	|6.|65536|1048576|29|_FileOptions.SequentialScan_|-90.69|00:0120976|
	|7.|524288|2097152|15|_FileOptions.None_|-90.68|00:0121058|
	|8.|32768|4194304|8|_FileOptions.SequentialScan_|-90.65|00:0121478|
	|9.|4096|2097152|15|_FileOptions.SequentialScan_|-90.63|00:0121732|
	|10.|65536|4194304|8|_FileOptions.SequentialScan_|-90.63|00:0121789|
	
- .NET 5 (Core) + byte spans + Access types 1. + 2. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/third_test_filestream_biggerblocksize/Write_NetCore.md)

	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:1514422|
	|1.|16384|1048576|29|_FileOptions.SequentialScan_|-92.11|00:0119449|
	|2.|1024|4194304|8|_FileOptions.SequentialScan_|-92.08|00:0119917|
	|3.|32768|4194304|8|_FileOptions.SequentialScan_|-92.04|00:0120543|
	|4.|524288|4194304|8|_FileOptions.None_|-92.02|00:0120794|
	|5.|2048|2097152|15|_FileOptions.None_|-91.98|00:0121445|
	|6.|16384|1048576|29|_FileOptions.None_|-91.97|00:0121616|
	|7.|16384|4194304|8|_FileOptions.None_|-91.97|00:0121617|
	|8.|4096|1048576|29|_FileOptions.SequentialScan_|-91.95|00:0121827|
	|9.|65536|2097152|15|_FileOptions.SequentialScan_|-91.95|00:0121834|
	|10.|262144|4194304|8|_FileOptions.None_|-91.95|00:0121958|

- Conclusion

### Read	
- .NET Framework 4.7.2 + byte array + Access types 1. - 4. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/third_test_filestream_biggerblocksize/Read_NetFramework.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:0693528|
	|1.|8192|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.30|00:0088072|
	|2.|1024|131072|229|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.22|00:0088628|
	|3.|16384|524288|58|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.18|00:0088885|
	|4.|131072|131072|229|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.18|00:0088898|
	|5.|16384|262144|115|_FileOptions.WriteThrough_|-87.16|00:0089060|
	|6.|131072|131072|229|_FileOptions.WriteThrough_|-87.14|00:0089171|
	|7.|1024|262144|115|_FileOptions.WriteThrough_|-87.14|00:0089188|
	|8.|65536|262144|115|_FileOptions.WriteThrough_|-87.14|00:0089192|
	|9.|32768|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.12|00:0089313|
	|10.|2048|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.12|00:0089314|
		
- .NET 5 (Core) + byte spans + Access types 1. - 4. [(Complete results)](https://github.com/thoeltig/CodingChallenge.1BRC/blob/develop/data/third_test_filestream_biggerblocksize/Read_NetCore.md)
	
	|Position|Buffer|Chunk|Write calls|FileOption|Time reduction in %|Time|
	|----|-----|-----|----|-----|----|----|
	|Base line|1024|1024|29297|_FileOptions.None_||00:0704307|
	|1.|2048|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-88.12|00:0083699|
	|2.|1024|262144|115|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.61|00:0087258|
	|3.|1024|262144|115|_FileOptions.WriteThrough_|-87.52|00:0087911|
	|4.|2048|262144|115|_FileOptions.WriteThrough_|-87.48|00:0088184|
	|5.|65536|131072|229|_FileOptions.WriteThrough_|-87.48|00:0088207|
	|6.|4096|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.47|00:0088221|
	|7.|32768|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.45|00:0088392|
	|8.|32768|131072|229|_FileOptions.WriteThrough_|-87.44|00:0088446|
	|9.|1024|65536|458|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.43|00:0088549|
	|10.|4096|524288|58|_FileOptions.SequentialScan_ + _FileOptions.WriteThrough_|-87.42|00:0088577|
	
- Conclusion	


	
> [!TIP]
> Additional informations on the topic:
> - [Win32.CreateFileA](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea)
> - [Win32.ReadFile](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile)
> - [Caching behaviour](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea#caching-behavior)
> - [File buffering And _FILE_FLAG_NO_BUFFERING_ requierments](https://learn.microsoft.com/en-us/windows/win32/fileio/file-buffering)