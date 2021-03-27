Simple implementation of UUID Generator, inspired by Snowflake, prototypes of UUID V6, 128bit.\
Sortable, no V1 MAC addresses, with sequencing, with node id, with random bytes, with UUID v4 version.\
\
First 64 bits:\
48 bits for timestamp, 4 bits for version, 12 bits for the rest of timestamp.\
|\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_TIMESTAMP_________________________|VER\_|\_\_TIMESTAMP\_\_|\
11111111\_11111111\_11111111\_11111111\_11111111\_11111111\_0100\_0000\_00000000

Second 64 bits:\
2 bit variant, 14 bits clock sequence, 32 bits node, other 16 bits are random\
|VA|\_\_\_SEQUENCE\_\_\_\_|\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_NODE\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_\_|\_\_\_\_\_RANDOM\_\_\_\_\_\_|\
10\_000000\_00000000\_11111111\_11111111\_11111111\_11111111\_00000000\_00000000
