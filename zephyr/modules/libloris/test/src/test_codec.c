#include <zephyr/ztest.h>

#include "loris/codec.h"
#include "loris/models/report.h"
#include "loris/models/uplink.h"

ZTEST_SUITE(codec_downlink, NULL, NULL, NULL, NULL, NULL);
// ZTEST_SUITE(uplink, NULL, NULL, NULL, NULL, NULL);
//
// ZTEST(uplink, test_encodes_and_decodes_report) {
//   uplink_t ul = {
//     .payload_type = UPLINK_PAYLOAD_TYPE_REPORT,
//     .payload.report = (report_t){
//       .sensor_id = 2,
//       .which_value = REPORT_VALUE_STRING,
//       .value.str_val = (str_t){
//         .ptr = "hello",
//         .len = 5,
//         .src_type = STR_SRC_STATIC
//       }
//     }
//   };
//
//   char buf[1024];
//   size_t bytes_written = 0;
//   zassert_equal(encode_uplink(buf, &ul, 1024, &bytes_written), OK);
//   // TODO: how do i even test this shit
// }
//
// ZTEST(uplink, test_above_cap) {
//
// }

ZTEST(codec_downlink, test_encodes_and_decodes_call) {
  downlink_t dl = {.payload_type = DOWNLINK_PAYLOAD_TYPE_CALL,
                   .payload.call = {.function_id = 2,
                                    .which_value = CALL_VALUE_STRING,
                                    .value.str_val =
                                        {
                                            .ptr = "hehe",
                                            .len = 4,
                                            .src_type = STR_SRC_STATIC,
                                        },
                                    .has_wake_after_secs = true,
                                    .wake_after_secs = 3}};

  char buf[1024];
  size_t bytes_written = 0;
  zassert_equal(encode_downlink(buf, sizeof(buf), &dl, &bytes_written), OK);
  zassert_not_equal(bytes_written, 0);

  downlink_t decoded = {0};
  zassert_equal(decode_downlink(&decoded, buf, bytes_written), OK);

  zassert_equal(decoded.payload_type, DOWNLINK_PAYLOAD_TYPE_CALL);
  zassert_equal(decoded.payload.call.function_id, 2);
  zassert_equal(decoded.payload.call.which_value, CALL_VALUE_STRING);
  zassert_str_equal(decoded.payload.call.value.str_val.ptr, "hehe");
  zassert_equal(decoded.payload.call.value.str_val.len, 4);
  zassert_equal(decoded.payload.call.value.str_val.src_type, STR_SRC_SLAB);
  zassert_equal(decoded.payload.call.has_wake_after_secs, true);
  zassert_equal(decoded.payload.call.wake_after_secs, 3);
}
