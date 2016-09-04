using System;
using System.Collections.Generic;

namespace mmchess
{
    public class TranspositionTable
    {
        public const int AGE_MAX=2;

        int SizeInBytes{
            get{
                    return 67108864; // 64MB
            }
        }
        TranspositionTableEntry[] TTable;
        ulong KeyMask;

        static object _lock = new object();
        static TranspositionTable _instance;
        public static TranspositionTable Instance{
            get{
                if(_instance == null)
                {
                    lock(_lock){
                        if (_instance==null){
                            _instance = new TranspositionTable();
                        }
                    }
                }
                return _instance;
            }
        }

        TranspositionTable(){

            ulong itemCount = (ulong)SizeInBytes/(ulong)TranspositionTableEntry.SizeOf;
            TTable=new TranspositionTableEntry[itemCount];

            for(int i=0;KeyMask < itemCount;i++)
                KeyMask |= (ulong)1<<i;
            
        }
#region HashKey Random Values
        public static readonly ulong[,,] HashKeys = new ulong[2,6, 64]
        {
            { //white

                {
                0xd19da6440dc85ebb,0x8c8446d842369c8d,0x46103af2eee5d8e0,0x4a183436e67c60e1,
                0x5e8a4435957c48d3,0x4ef2f3a9d6c9ebb0,0x834eca42a71c4440,0x3cbc421d0d8e0cc2,
                0x82ae2c9747bc2a17,0x490cf8343314ddc5,0xaca14bbe32569657,0xaa4efeee72671947,
                0x72decfa84a5d243a,0x21909340cdb0a436,0x42bfb0fe3fd8b5a1,0x7511c3e1909d431f,
                0x8dc6e0b94b32fef1,0x40642f803bf5242b,0x1210b4f3ad4e7f0b,0x4d396452a54bb0c3,
                0x28024311db8d4e6b,0x55a34f7c5f143c0f,0x4a3c8e47b40a14b7,0x4b7e9afcc1ed5091,
                0xe48c8c4ca4ed4763,0x9b12984e15b6f9e9,0x884119fccf23a647,0x1c26bd4a821f698d,
                0x4baee2470f417ebf,0x96bd74ab92884585,0x3e9b4012c8a356e9,0x6b269681445ddb74,
                0x9e42a4fbb107978a,0x339e4f3cc93d249f,0x61a50490407a7368,0xa2544ecba543ae49,
                0x7354a1471b23f05c,0x927ab843133155d0,0x7afa67ac4a2642c9,0xe9738887496951f9,
                0xf76755898f48088f,0xac0db2b54832e5a8,0x5a9d4783b5dbe0f9,0xa954cf19a4850d0,
                0xfefc5daa42348b15,0xa85fce20f2934dfc,0x149cc89ee98d984a,0x884c29e9ee78876b,
                0xd62e4488408e4363,0x4068c8b2bf4e4a58,0xa60532a02aac4702,0xac4579182a2a70a6,
                0xef96a44ae1a18c0e,0x4568ee01dcb1cbf5,0xc1f7ba483ed3994b,0x8e492e3bd1be4df3,
                0xfcb123d8bedfb143,0x4133647ea0568844,0x3df369fa9c470ee0,0x93850f6f01874afe,
                0x42d05f339117744f,0xfec9ba5f9346c890,0x9a4e5515f6292469,0x3092b894a2a9d,
                },
                {
                0xeb9f8d488d43523c,0xaafc935cb34faa82,0x38f50d335f18bd4e,0xb74ef38bd32cfb03,
                0x91b4abab37c1964e,0x43a6b60d7f6f638f,0x4f8e4bca77187baf,0x8a43f764d342e5ba,
                0x7fcda142de51a2e9,0x44fc47d21eb28fd3,0xe71c9b4bfba81ddc,0x77934927a1bd7011,
                0x429098780f2d4010,0x4212ad95fed8feb6,0x368f4ea646170d68,0x8ffebd9846ffdbc1,
                0x7859b44f4927e08b,0x582ac334aa4c688a,0x1b434c99999f48fe,0x3a359f48e74373f3,
                0x1ccddc1a8621b642,0x8e4089b99dc07bc3,0xb041801d07554fc6,0x44ea724e9a94d094,
                0x753692437d94ff7f,0x5b2435773a43540,0x45ba26a3a722243b,0x3ffc9543ae40f6c8,
                0x61483b60b842399a,0xb570a79fb148db8f,0xb25cac42e9d55f4b,0x3ca21c8ef04bb24b,
                0x7d3ba5422bb8bedb,0x508af19e4350da39,0x9a819c814309f9ee,0xe9932336f2a68046,
                0x340b4c35979044f0,0xd3eaa1f402ac47e7,0x51ae4b5fa90fbe1e,0x39463a95414ff801,
                0x3d8d81401959b6ee,0x24f2a5eb96409dc8,0x960fb910af4182d2,0xb0b24e71dad3add5,
                0xa3802baba8400db6,0xab79da46bd4d4d6d,0x45b2a8457c94e069,0xd6acbfb790a142a5,
                0xf8ae9e475933ddf8,0x4c3a84f4abd24e09,0x56a14bb5d8aaf8d7,0x825db34d741ade20,
                0xb76747bbabf4b844,0x438cdbc8b3b34c0b,0x348caacaa7435fe4,0x6b4d3aae416d93a6,
                0xf60ab7b74bc89e96,0x23a5168fcb984828,0x8dc546ea6baf4188,0xb18fbd974d3c2c9a,
                0x4757959a8b8e0d13,0x5aba028e5eb14911,0x756f834fd3023b3b,0x43ee6a06aa4f89cd,
                },
                {
                0x19489b42a2488434,0xd44d33874a314496,0x3b0f37d519af4d4c,0xd9488a48f97ea862,
                0x67808dad4bf8c820,0x8a7383854110a0b4,0xbf392c2ebca14b93,0xc2190fc9a44fd420,
                0x946c8e4d56685fff,0x768d4b5560f13c81,0x47f39280a4636f48,0x2ab1472a38c68551,
                0x8e99f6b44d985082,0xc535441ffa347d0,0xa338a444435043f7,0xce3b1e318e4108f0,
                0x8c98429beefad7ba,0x8a44fe4b7936a0b6,0x484c007ac736a640,0xbc7b146bb34e3aee,
                0xec2ca74a64ce5b7e,0x40af037e5162b583,0x41591b978955a017,0xdeac27a14d0e0704,
                0x9f584db067844d76,0x2347cb52b7318a40,0x17ab9a42ceb3b832,0x2821f68c514b9148,
                0x470e06e89aa7121f,0x28a9df9fbc4445eb,0x58da844fb093c523,0xeedfb3b24d15dc22,
                0x994d90fac30d7406,0x76e66c80dbe58d4f,0x79580c2595450ad8,0x4dc261b0f2b92d4f,
                0x6ccb02693eb54fe1,0xbb56b0e7af426fae,0x52b5464b75497bd6,0xfad7e0f66a8b9c47,
                0x48b547b0a49c39fa,0x851c98b240867fc0,0x2f6ad163db86af45,0xdc3379b404d95f0,
                0xa79c6f7b9d4d3ba6,0xa152a34692b8756d,0x4f0035ff6ba7c90f,0x252216aaa4b04fff,
                0x8d0eb94a6988c30a,0x4ddf4c6cbae8523d,0x286499a83b35f2e,0x9540335ee4824cd4,
                0x46b18a694ea3400a,0x9fcf922321b74bd3,0x9c8e4326a46ff74a,0x30d2a2ba4a5a1190,
                0x9705079247470869,0xe944821c5b5927d,0xa94d1f539541c134,0xdae777a94cb6ce3f,
                0xb9414db4bf286b66,0x44f4a0f60a748b44,0x8b6ff8cab74e58f7,0x536fdb448144a37a,
                },
                {
                0x1d7b408d133cb441,0x853d95468ee3ceab,0x468eb9e68cb16704,0xea3add9b4e33f4fb,
                0x4d7a75fb357afa49,0xcae1a14e5b264c58,0xe974819cbaad4d7b,0x2db21db945d9a964,
                0x78b2efd5e9a7824e,0x4bdd957f38da9483,0xadeea14ddc16cef3,0x5efadd716f984d2a,
                0x844a4ae8fa38c436,0x5c6fb448ad12b99d,0x3e510ed14b0e8946,0x59be4ed039a9ce86,
                0xed398b10ab6bb643,0xfdf4cb9c4706076d,0x5e2387a04f0ad6f8,0x614f881b277b9a4b,
                0x615cb04f290b4a6c,0xafe1619b4e99dca3,0x77710ee9f1ad48f5,0x31e09246ddaaf322,
                0x1f42c3b54d2b1483,0xa04df421aafbcc2c,0xc1dfc1b94972c1bb,0x1b9c07914122a130,
                0x262f4fbc9bc5944c,0x59262947924eacf1,0x866cfd1aab462c03,0x7eba4eaf96df3ee8,
                0xcd1b90cab7b6944c,0xa54224ed34585f8d,0x4c89e613af4dcbad,0x87b84c8862288451,
                0xedb49647a15ae469,0x2e018f407f1aa93e,0x7110fd088c4d2d83,0x4f995fec1c8de196,
                0x5d9544964a8dceb1,0xf336c5a3ac4df1d1,0x4d50f7af4c2ca048,0x785059c1864fa586,
                0x7b0ab542415fa46e,0xc8458904f655b0b,0x73b8b043c579c786,0x47704f2102f686bc,
                0x8c8c4424edbd0e9b,0xae9641738e514fbd,0xdeb6ca9d4aba25fd,0xfbd538ad4d789a5e,
                0x6ab94c68db229cff,0xd1d7260c7a9e4a2e,0xbb4e91bc59fd507b,0x3cbb4df4700bef0d,
                0x9bdbe4f4b54713c9,0xf7f5eba0495631e3,0x49520e5b93e6fd60,0x3a57be63a1bd4a90,
                0x42cc73b95f3bbc43,0xa44da3147cfab22d,0xbb63f2019e4f0114,0xbb4887924bb1a2be,
                },
                {
                0x8c4db84f42ccd967,0x4b9d4b3b18a56fab,0x44a7cdb7c671a144,0xc0c252c9ac178744,
                0xf3979ed71c99409e,0xb636f7b344c40f31,0xc9b585d773d18149,0x3a031b9e4d92ebf1,
                0xffbc0f05912dbf44,0xfac5779548a3ce92,0x5e372fa440dc395d,0x80fb46603fb84941,
                0xd7a9da9c47e860c0,0x43ac6689db2ee5c8,0x4d304c320720ca2a,0x48e936af84c75294,
                0xc7f2a89e549c463e,0x93c9924d155a3086,0x8341fcd2e615b5f6,0x8c8e7535b7441bc7,
                0x4abaa251e5a60c87,0x43072cfeb6489ac6,0xaf61385986008f40,0x4f353aa7d66c476a,
                0xd8bf12e61aa48a42,0x5ed28342806c8025,0x6f15cac4560f8fe,0x5ce4504a60ba435e,
                0x67bb479651bdb26b,0xbbd13df102a7bf41,0x994d43f80ee90daa,0x4ec656d3cb084ab6,
                0x4dd8ccec409552a0,0xa69bb8d1c489485c,0x439e4b48f694848e,0x7db44611b40ff7e3,
                0xb3c32091a54fb7dd,0xa7908bb0406aadb8,0x11a448a7127a6d28,0x9a0172ce8b476614,
                0xeb96ae48566bcc3a,0x4ee84a1de2fe37a9,0xe77dc3c594437d47,0x1b4857d721af4849,
                0x8042fbe00fba5d9d,0xb23b9342b739db7a,0x7c9317bca743c109,0xf32701b8b45317d,
                0x72d8f7ac41b7060e,0x47847a2104c34dd5,0x46a6cb6fdbf6c182,0x910680432f2f0256,
                0xe628ab22effca049,0x49671bc49520d078,0xa941f665bc9ad41f,0x2aa441fb127a5a13,
                0x472158885a81edc5,0xb4704832038541ee,0xb7c5ebed20e29b4b,0x99ac81477bc16803,
                0x42f93b7f3ebf6490,0x4ba5131e5d51304f,0x874f15b4e2d0abb1,0x4c279031c81c8b19,
                },
                {
                0x1cd5884e2e043d89,0x28c9ab4123f4c69e,0xf9c6ab1cccb84e3d,0x2df47e5e25be4800,
                0x1ed3a6e52f884e37,0x4ce22a9ba14bece2,0x354a8c43252f65ff,0x7cae4935f51ce9d7,
                0x4cc54317a77ba56f,0xe10e854491e02107,0x9d4b8ead9ece3cc5,0x494cbdf920ac5dd2,
                0x928c4f8914815f3f,0xbc4f38d570f83a30,0x6589178906834c03,0x43cb4e9142578324,
                0xad75864eb64320d2,0x78639c3328f88143,0x12a84a4ca69e0e03,0xc38444e05ddbec54,
                0xc787a9870c9e4359,0x831f7d21b447b2eb,0x4473a875d6894967,0x4e64600b2b152a29,
                0xf20cf09e423f9f21,0x9451e76d6c33be4b,0xb8b347ed06aaf8ae,0x97dda3ad8da5447c,
                0x5549ad8b466ef18c,0xf2e99a196aa79242,0x363361e8a14cd148,0xfb44203c18b31ea,
                0x946c6225d9b84534,0xc47a6fb7894de118,0xaa7e9a4c45907228,0x9dce9757964cdcd9,
                0x42c0042eb1491545,0x211b4953458e497f,0xaed79879d6b048ae,0x93fbeaa9c6ab465d,
                0xa9c8709446674d09,0x1662b6458eb34a45,0xffd59e403fe30b68,0x77d5b9404853a197,
                0xfb7ff6b32aa841ce,0x9d415f0a33e5b17b,0x4044cd8def6417a6,0x63760efcaa11a449,
                0xde7e5ddf16eeb348,0xa51da4419be959b0,0xb23dda958740f24a,0x445649fe5468564b,
                0xfc8b56d0ccac4d3f,0x2afb54f2d2d98446,0xbf483760ee98966c,0x4d37492fd5914846,
                0x89528d4c4b142f22,0x144ccaf6ee874ea2,0x81421f2983939c78,0xf2184c7e71a34ab6,
                0x5fa14aa1864854cc,0x74ac5ca9425cf577,0x373a1bffa94156a6,0xb4402a9411464cbf,
                }

            },
            {   //black

                {
                0x9dea904305f5e099,0x6f5d8a400bece6ed,0xa52490d1b34ccbae,0xfbcf81c88f4187da,
                0x954e4b68ecd8d761,0x809a48655cbfad83,0x87f9f1cfa0904d7d,0x813983482c44f6d2,
                0xf0dcab4668bc4487,0xa1b3c7cf188e8c4f,0x4fb04d8166b6396e,0xd59e26be49ee0b2e,
                0xe25f6e3157338b4c,0x8a9c8e4ec8b5f68c,0xb691429ee3e8db55,0x1d9b4236c1107d2e,
                0xbe0ab001c487a644,0x1c2095017fbd407d,0xeb864c8654047226,0x93775ae630874422,
                0x8e4bbb7ee11aee39,0x423e63941cdfcf39,0x1a154bd601c59b4d,0x6efc874f2faf0856,
                0x5d56b6f1f8fa9e41,0x78f295d28943c582,0xbd452412baeceb5b,0x69e4ec3bd13d99e,
                0x90772e56854e6cd9,0x81fdd09a1d87ab4d,0x8304235712bfbf44,0x2f58a46842a15cd,
                0xc2a45eb9472ab0ca,0xac15b063fc689e4f,0x408766bc7af3a51a,0xfa4ef1a54bcec7be,
                0x24ae40b3f6bafae4,0x544bc5bffc9c4d6b,0xc1a7f3938e59b54f,0x465611a7c0cd0df3,
                0x3a41884dcc1dade2,0xfb9444192086456c,0x8ee4d4fa86465659,0xfeb84cda7cca8d30,
                0x4b77e8a9e256ab6f,0x91570f97be49856a,0x3c3134a563a74740,0x1a2baa4bc7617c5b,
                0xfa9f4d1a1c197c26,0xe302b84e69771a7c,0xc026a34838edb64e,0xe42282415e78aa5f,
                0xbca4bf486da02b10,0xd2a5a04a428ecd27,0x41e4aa3b63a16281,0x408a4d0295945531,
                0x386c978345311784,0x3fb141e1d2f368d4,0x6f6f52e2af427645,0x581fc690425322e9,
                0x98418df6f9801e34,0x8a7390482769db5c,0x73f2ecf574a24216,0xa1d89d4cdc69e94f,
                },
                {
                0x8d1234d66a26ba4e,0x9e3599995d914ee2,0xe775bd6a88d6a346,0xb32330f812339b49,
                0x608ebb463e493d3a,0x9e4f0cef88465fde,0x5e022408990e964e,0xbc444f77e6bb408f,
                0xea8c0a70db824bec,0xaf49f832eb20a937,0x3a3b4413c69adf2,0x61f37dffa8894001,
                0xc751be4bd8995ba6,0xafd405964415e6a5,0xfbda8a429e5ab441,0xa048decce76580c9,
                0x3cfe2006b7b44ecc,0x29acf0379941a92b,0x46fb409942f2669a,0x1b804ab85063434c,
                0x7d33e9a04ae62bd3,0x7f8d9d4afd460adf,0x4917de750ad3e3a2,0x4801cc4589d05031,
                0x5463b04032158d42,0x75dd93c0989e4505,0x45b36edbf51b4016,0x4aee2d108f011fd0,
                0x4892cd825017774c,0xaf2e9d41240d4ed5,0xac0a38b9414ba546,0x3e2f3e19b6deb944,
                0xcd2a43904eea4ca2,0xac4bb54c7d7e8a49,0x234e9b43c6563bc5,0xacb54b51c698b7d2,
                0x99af9cf3fc874211,0xcbccb680304c884f,0x468a1900da3eef07,0x3ee4b66a8345dc24,
                0x9b359f4ac7fcb5f4,0x2bac48c897fcd81a,0x56ff33bb8e4a71e9,0xba4e8a7b9196348c,
                0x41361c3a4b1e71e9,0xae44645ce8ffd3a7,0x7812889502924a1b,0x488501d84df7b144,
                0xa8d3f9255b639a41,0x418ba64dcd697dba,0x3cf1d08184464adb,0xb23497428fe6d55f,
                0xd24cbc4201338543,0x9d70acb74cf824c6,0x4ded85fd15aeb14c,0x7a3a92420c9f9771,
                0x7ccb792a69145fb,0x53d998bdbc844e60,0x4d6f9b066a369b02,0xe2ec37a7b543dc96,
                0xbd8b6c914fd0cdd6,0xe757f26ee983b642,0x42c96c9f19052368,0xc7894970b9c2a428,
                },
                {
                0x39b247c55717bd2a,0x2bbbf38f47254d02,0xbac472b0591d898,0x358743b2625af7db,
                0x4e1f81b4161440e6,0x4d00293fc0b1c391,0xdd2a9437cece090,0xcd15309416d88d48,
                0xb7100d9f84446a17,0x908b9d4b97cac7e5,0x97672442d29b48ef,0x914a54bdd74de4fa,
                0x82e2a418608e45ab,0x40f1549d91aa4f37,0x4af9be6cfd67c37b,0x227bac77b048042f,
                0x470a44d96a30bc46,0x42a61033629923e8,0x43017fc4092e5a0f,0xa67977519c4d64ab,
                0xd52c0af88647aa22,0x8bb8afaa4a6cb2ce,0x2d879e4e6784d89a,0xb4091b586d478d46,
                0x90496cac55366a2b,0x7e8981ff22edbd43,0xf9028c498b97fd95,0xff144f1012b09546,
                0x245cef484664ab4e,0xdc3dbe4d21503109,0x403b88738ec59bd4,0x6c6b9f4fa97bbb08,
                0xb348a17bc0f9b517,0x879b4f7a5ce2b670,0xbee4a43811d8a444,0x5b386c9740e32e14,
                0x2942a915398747b2,0x13c88641cf3ba71b,0x4635e23a10a94463,0x37a1f220f8a24428,
                0xb18d78c49c8b649,0xdb151ffa6f9846b0,0x91b88833b84fd4d2,0x4da4122f45e27958,
                0xbecf5e30bc4a2932,0x4339ca15834dc08e,0x6924cf47b447ce51,0x4746eff531ff48bc,
                0x5978c9fcf9bd4a43,0x31924071d4dc39f1,0xdad109c097452373,0x45b9629e44c3168f,
                0x858ec27f4a441fd,0xed13ab4598ffa055,0xf40e43bc42c9c878,0x4d385aff804f9400,
                0x4fe3387a71a27bb1,0x25ae4538d8881e42,0x4e308b14bad7685f,0xa34405e48aadb7bc,
                0xd28ad8a8b245f9bf,0x46077991492d2f8c,0x476e82c97f9ccdbf,0x72ad4154023a8c31,
                },
                {
                0x1763944bebe5854b,0xfca684419a38aecf,0x425c01937fcd4a74,0xb34da04f7f4a9811,
                0xa5892c8e4bb748cd,0xade07890464ba0b2,0xbab2fc0797834ec4,0x1824b4435f9c27be,
                0xbd48756a62172369,0xe8d4d1edf0cab92,0x30bfafb09542a093,0x4663ae0e3055b042,
                0x16713818e7b54a94,0x7416bf44d89869d2,0x14a54ac2db62b0ea,0xe2c396984efaf0ba,
                0xb34fe3beddd3dbc3,0xcd25bf45beaa2e4b,0xac4eba2c03c68b50,0xec0b5592462fb65a,
                0xd88954eac4b6a69,0x5c868f48dda7d482,0x74e864bd4e7f1c1c,0x77fb5eb13a824fff,
                0x8e96067685412d16,0x8049c67b4032ad2f,0x9be7b84286cc1d6f,0xe4cd698e4ea4c867,
                0xcd4590494484592f,0xca30db7a8e46ed0e,0x9942fb5fa87b3226,0xcc9d864f5f6c3b59,
                0xf4d0c9c6ff934f3e,0xc18ead491ec9a47b,0x97682875b0438568,0xab4f8e16b2faf924,
                0x69a63b328845e4ef,0x2b9e49a61afdf4d9,0x9244bde99a6456d7,0x2b864f09c96dbbb9,
                0x6a1fa84c1651dddc,0xac40065d740b07a3,0xfabd42ec828e25ca,0x3bee16bf4dd0eec5,
                0xa6d46c98ae42d752,0xd43284f9f45e4da,0xb86df157ac4411,0x8a95ba819048f3e8,
                0x49ed093800521fee,0x48b3cf8f4087ccb8,0xa6725d4b4b7f904d,0x8c450a2c0f4e090f,
                0x5d5fbb4685e2ffca,0xcd2ad4e07b2e18b,0xf9ff14824b72d832,0xeffa0f7ae20eb64e,
                0xdf93a1dbfffdbe4c,0xaf4ff47a69851cbb,0xc11249fc8848bb3a,0x94e9a79840d9e477,
                0x418ee40b275a7fad,0x639143b2055e2770,0x40e040cdc36a81d3,0x77c76aa04cc89446,
                },
                {
                0x8a4f7fd4c4378b04,0x8047d5f2102987c8,0xbd433550ce89d73e,0x25668f49be38acc2,
                0xa1231543be402266,0x435b16ce4284cbbb,0x163b3bfb85994399,0x3d01ec5c82ae9e43,
                0x4b96470797cbdad9,0x334dbd637e60aa45,0x48e79f8166bb4cb9,0x4c16aa8f424656de,
                0xb64afeea94108acd,0x41688d8d9a2aad74,0x44f18119439cb3c7,0x41f3b2997d7a5966,
                0xaa1db4494d21827a,0x881eb7878a4d7503,0xff5ba549d94d3fb1,0x5ada467ec6fa54a,
                0xffc73799430411,0x4a7087b123b4b641,0x4f1037d5b3e95fe1,0x27a14a1a9bc831cf,
                0xdd4bcc05b381b84e,0x3c90b1b30da6409e,0x4785421b1ffe5fba,0x1fa39d4c4c12273f,
                0x792363ab45260349,0xa8c484be46c4a84f,0x874185d16bd5455e,0xa14e508d265917d4,
                0x8b450d4ad0ae0a5e,0xf72fb79fa3904725,0xd8445035a9ab093,0x81ef9743664eb094,
                0x112b9947730db0b6,0x48df8c4e25dc4962,0x87d9458a05f1a045,0x3192478923329941,
                0xfc538f628445ff85,0x43f865173b578658,0x155ece5d96fd8641,0x2ea8be4042a8f74f,
                0xcab14a3c806fcb80,0xf522f6e29dadbc4d,0x1e37ef94964a4ce7,0x2711f99fcda9439a,
                0x9647ac12b5912e64,0xa8f7cd838e4581c7,0xa644c98a426b31d1,0xbc8147da7a3c30bc,
                0x7a8def4d1592904b,0x412df6bd7d3a5c08,0x1690cd563ab34c5b,0x939444b005e9f443,
                0x9e41435b4d5d3fff,0x9cb0ea68cfa94177,0xb2e103a74e1fa7a1,0x422bee8d4ebc9946,
                0xb2271b23b54012f8,0xdaf89b4d73a1790d,0x8f6c9143ab3e4f24,0xdf8ac6aa8b4061dd,
                },
                {
                0x27e97d828e4d1db0,0x6f117546c58f9d4c,0x3cb2446264d11d6e,0x6a0af4275f425a6,
                0xb703992e804b47d7,0x5c19429448ac5dbd,0xac26595b81478359,0x4a55fabd25ecbb76,
                0x8bc766cd79804a90,0xed329c4f918a5fe9,0x95884b50cc982830,0xb6ff9eb2b84aecf8,
                0x402903c06d5360fd,0x439550bd4505defb,0x48102dc94001fe31,0xfafcbf00fd26874f,
                0x4e110e01da21ecd2,0x205e9341fd44c2b9,0x18a341f3394ec906,0x3aa06c4e8d8bb34c,
                0x1918944f3646fb5a,0x81900ca122874ccf,0x9b4c3095f8d2b2af,0x494d15c3f0d4727b,
                0x24fea3a189a09e4a,0x4d9d8a1da3d4d65f,0x8fa643b4b55e4b4a,0xa8be1eb945af5d76,
                0x4fd0b88643944d02,0x4e14653e56e84302,0x88af48d75799490c,0xbfaf49a06ef67c9c,
                0xba51588243406673,0xaa44596749a44ab6,0xb2eeadef9c4b5ce6,0x3baf47d26bf338e2,
                0x13bc4e8041be03f6,0x88bb4787b305ccec,0x9a44f671bdbb0f4b,0x76be78ad4ada3a4e,
                0x4955655ec5c80f6a,0x8144ec5e7d50a502,0x2293bb9e44692a33,0xa060b14d9d0bd799,
                0x763d449b98a64818,0x4ce43f2be3641d7f,0xbb42387f1294d316,0x576b8ab4253c4d3,
                0xa1fd587aeaaa4630,0xf27cdcc2a54bdac8,0x6c1ca331bdc88d47,0x8e403613336145fc,
                0x4f6760cc5397968d,0xa09ab4e6c0ab1f7,0xc5914adfce6be966,0x6569b2a974d99349,
                0x8d2385208e48d5b9,0xafbb46988f7bec11,0x5f21b546282772f1,0x1895c9ac402b773f,
                0x57bf496734c808c9,0x40836393eba0230d,0x934bc5c6237b123c,0x1a41cfe13ab4020,
                }     
 
            }
        };

        public static readonly ulong [] EnPassantFileKey= new ulong[8]{
            0x4e3c453a85cf1c28,0xd2b3a74837aa1b24,0x9d0efb01be69a74f,0x649941f3f6b99925,
            0x8464d0b1a1c7a840,0xd29147a7c448b209,0x547c914dae8833d2,0x4ea326614b526181,
        };

        public static readonly ulong [] SideToMoveKey = new ulong[2]{
            0x1c9e7aff5f9e45ef,0x4c45362999b2481e,
        };

        public static readonly ulong [] CastleStatusKey = new ulong [16]{
            0xc7531492d2deac48,0x29bfb6b94690e79d,0x7064484865528a46,0x4098158749f2c173,
            0xb5c5fd8a4c62c9ef,0x221c256f338d4b35,0xc8d4d39e20decbc,0x7680414afc2845f5,
            0xfb824249eb0365ff,0xbf6df355b04168b7,0x947e6b5b569d944c,0x4b9c2efab42fcae,
            0x4f14b3e323ea6422,0x9b438b6680100598,0x18ed5d58ae43cf1b,0x7918396af069a141            
        };

#endregion

        //this code was only used to generated the static numbers
        public static void GenerateHashKeys()
        {
            var rand = new Random(DateTime.Now.Millisecond);
            for (int s = 0; s < 2; s++)
            {
                for (int i = 0; i < 6; i++)
                {
                    Console.WriteLine("{");
                    for (int j = 0; j < 64; j++)
                    {
                        var bytes = Guid.NewGuid().ToByteArray();
                        ulong hashValue = 0;
                        for (int b = 0, index = rand.Next(8); b < 8; b++)
                        {
                            hashValue |= ((ulong)bytes[index++] << (b * 8));
                        }

                        Console.Write("0x{0:x},", hashValue);
                        if ((j & 3) == 3)
                            Console.WriteLine();

                    }
                    Console.WriteLine("},");
                }
            }
        }

        public static ulong GetHashKeyForPosition(Board b){
            ulong hashKey=0;
            hashKey^=(ulong)b.SideToMove;
            hashKey^=b.EnPassant;

            for(int side=0;side<2;side++){
                ulong subpieces;
                subpieces = b.Knights[side];
                while(subpieces >0 )
                {
                    int sq = subpieces.BitScanForward();
                    subpieces ^= BitMask.Mask[sq];

                    hashKey ^= HashKeys[side,(int)Piece.Knight,sq];
                }

                subpieces = b.Bishops[side];
                while(subpieces >0 )
                {
                    int sq = subpieces.BitScanForward();
                    subpieces ^= BitMask.Mask[sq];

                    hashKey ^= HashKeys[side,(int)Piece.Bishop,sq];
                }                

                subpieces = b.Rooks[side];
                 while(subpieces >0 )
                {
                    int sq = subpieces.BitScanForward();
                    subpieces ^= BitMask.Mask[sq];

                     hashKey ^= HashKeys[side,(int)Piece.Rook,sq];
                 }                
                    subpieces = b.Queens[side];
                 while(subpieces >0 )
                 {
                    int sq = subpieces.BitScanForward();
                    subpieces ^= BitMask.Mask[sq];

                    hashKey ^= HashKeys[side,(int)Piece.Queen,sq];
                }                
                subpieces = b.King[side];
                while(subpieces >0 )
                {
                    int sq = subpieces.BitScanForward();
                    subpieces ^= BitMask.Mask[sq];

                    hashKey ^= HashKeys[side,(int)Piece.King,sq];
                }                
                subpieces = b.Pawns[side];
                while(subpieces >0 )
                {
                    int sq = subpieces.BitScanForward();
                    subpieces ^= BitMask.Mask[sq];

                    hashKey ^= HashKeys[side,(int)Piece.Pawn,sq];
                }                
            }
            return hashKey;
        }

        public ulong HashFunction(ulong key){
            return key & KeyMask;
        }
        public void Store(ulong hashKey, TranspositionTableEntry e){
            
            var index = HashFunction(hashKey);
            var existing = TTable[index];

            if(existing != null){
                //verify lock
                if((uint)(existing.Value ^ hashKey) != existing.Lock)
                {
                    //we have a collision
                    //decide to replace
                    //replacement strategy
                    //everytime we hit, we age                    
                    if(Math.Abs(e.Age - existing.Age) < AGE_MAX) //absolute val in case of overflow
                        return; //do not replace
                }
            }

            //calculate lock
            e.Lock = (uint)(e.Value ^ hashKey);

            TTable[index]=e;
        }

        public TranspositionTableEntry Read(ulong hashKey){
            var e = TTable[HashFunction(hashKey)];
            //verify lock
            if((uint)(e.Value ^ hashKey) != e.Lock)
                return null;
            
            return e;
        }
    }
}