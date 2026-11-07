#ifndef LORIS_LORAWAN_H
#define LORIS_LORAWAN_H

/**
 * @file
 * @brief loris LoRaWAN API
 *
 * @note In order to use this submodule, the target board's Devicetree must
 * define a LoRaWAN compatible `lora0` node.
 */

#include <zephyr/device.h>
#include <zephyr/lorawan/lorawan.h>

#define LORA_DEVICE_ALIAS lora0

/**
 * @brief An enum representing all possible loris LoRaWAN return results.
 */
typedef enum lr_lw_result {
  LR_LW_OK = 0,

  /**
   * Unknown error occured within the Zephyr LoRaWAN stack.
   */
  LR_LW_ERR_UNKNOWN,

  /**
   * LoRa device not ready.
   */
  LR_LW_ERR_DEV_NOT_READY,

  /**
   * Error while starting the LoRaWAN Zephyr stack.
   */
  LR_LW_ERR_START_FAILED,

  /**
   * Failed to join the LoRaWAN network.
   */
  LR_LW_ERR_JOIN_FAILED,

  /**
   * Failed to set LoRaWAN region.
   */
  LR_LW_ERR_SET_REGION_FAILED,

  /**
   * A problem with retrieving/saving the devnonce occurred.
   */
  LR_LW_ERR_DEVNONCE,

  /**
   * Sending data failed for an unknown reason.
   */
  LR_LW_ERR_SEND_FAILED,

  /**
   * Sending data failed due to duty-cycle restrictions specific to the
   * selected region.
   */
  LR_LW_ERR_DUTY_CYCLE_EXCEEDED,

  /**
   * Selected datarate was invalid *or* ADR (Adaptive Data Rate) is enabled.
   */
  LR_LW_INVALID_DATARATE_OR_ADR_ENABLED
} lr_lw_result_t;

/**
 * @brief OTAA (Over-The-Air Activation) configuration parameters.
 */
typedef struct lr_lw_join_otaa_config {
  uint8_t* dev_eui;  /**< Device EUI (8 bytes) */
  uint8_t* join_eui; /**< Join EUI (8 bytes) */
  uint8_t* app_key;  /**< Application Key (16 bytes) */
  uint8_t* nwk_key;  /**< Network Key, required for LoRaWAN 1.1+ (16 bytes) */
} lr_lw_join_otaa_config_t;

/**
 * @brief ABP (Activation By Personalization) configuration parameters.
 */
typedef struct lr_lw_join_abp_config {
  uint8_t* dev_eui;  /**< Device EUI (8 bytes) */
  uint32_t dev_addr; /**< Device Address assigned by the network server */
  uint8_t* app_skey; /**< Application Session Key (16 bytes) */
  uint8_t* nwk_skey; /**< Network Session Key (16 bytes) */
  uint8_t* app_eui;  /**< Application EUI (8 bytes) */
} lr_lw_join_abp_config_t;

/**
 * @brief Join a LoRaWAN network using Over-The-Air Activation (OTAA).
 *
 * Configures the regional settings, initializes and starts the underlying
 * Zephyr LoRaWAN stack, and then performs OTAA.
 *
 * @note DevNonce is managed automatically by this function via non-volatile
 *       storage (lr_nvs submodule). It does not need manual tracking.
 *
 * @param[in] config Pointer to the OTAA configuration struct.
 * @param[in] region LoRaWAN region, defined in zephyr/lorawan.h.
 *
 * @return Result of the join operation.
 * @retval LR_LW_OK                     Successfully joined the network.
 * @retval LR_LW_ERR_DEV_NOT_READY       Underlying LoRa device is not ready.
 * @retval LR_LW_ERR_SET_REGION_FAILED  Failed to set the specified region.
 * @retval LR_LW_ERR_START_FAILED       Failed to start the LoRaWAN stack.
 * @retval LR_LW_ERR_JOIN_FAILED        Network join request failed.
 */
lr_lw_result_t lr_lw_join_otaa(
    const lr_lw_join_otaa_config_t* config,
    enum lorawan_region region
);

/**
 * @brief Join a LoRaWAN network using Activation By Personalization (ABP).
 *
 * Configures the regional settings, initializes and starts the underlying
 * Zephyr LoRaWAN stack, and then performs ABP.
 *
 * @param[in] cfg    Pointer to the ABP configuration struct.
 * @param[in] region LoRaWAN region, defined in zephyr/lorawan.h.
 *
 * @return Result of the join operation.
 * @retval LR_LW_OK                     Successfully joined the network.
 * @retval LR_LW_ERR_DEV_NOT_READY       Underlying LoRa device is not ready.
 * @retval LR_LW_ERR_SET_REGION_FAILED  Failed to set the specified region.
 * @retval LR_LW_ERR_START_FAILED       Failed to start the LoRaWAN stack.
 * @retval LR_LW_ERR_JOIN_FAILED        Network join setup failed.
 */
lr_lw_result_t lr_lw_join_abp(
    const lr_lw_join_abp_config_t* cfg,
    enum lorawan_region region
);

/**
 * @brief Send data over the LoRaWAN network.
 *
 * Sends a payload to the specified application port using either confirmed
 * or unconfirmed message frames.
 *
 * @param[in] port Target LoRaWAN application port.
 * @param[in] data Pointer to the payload (bytes buffer) to send.
 * @param[in] size Size of the payload in bytes.
 * @param[in] type Frame type (LORAWAN_MSG_CONFIRMED or
 * LORAWAN_MSG_UNCONFIRMED).
 *
 * @return Result of the send operation.
 * @retval LR_LW_OK                      Data successfully transmitted.
 * @retval LR_LW_ERR_SEND_FAILED         Unkown send error.
 * @retval LR_LW_ERR_DUTY_CYCLE_EXCEEDED Transmission blocked due to regional
 * duty-cycle limits.
 */
lr_lw_result_t lr_lw_send(
    uint8_t port,
    uint8_t* data,
    size_t size,
    enum lorawan_message_type type
);

/**
 * @brief Request a link check from the LoRaWAN network server.
 *
 * Schedules a link check MAC command to evaluate link quality on the network
 * side.
 *
 * @param[in] force_now If true, forces the stack to immediately send an empty
 * uplink, to which the link-check MAC command is attached, otherwise the
 * link-check command is sent with the next outgoing uplink.
 */
void lr_lw_request_link_check(bool force_now);

/**
 * @brief Register a callback to listen for downlink messages ON ANY PORT.
 *
 * A simple wrapper around the Zephyr's callback registration.
 *
 * To avoid dynamic memory allocation the user must supply a dummy memory
 * container (@p dummy) that is then filled (callback and port are set) and
 * passed to Zephyr. This ensures that the Zephyr callback struct remains valid
 * without any dynamic allocation.
 *
 * @note The storage provided by @p dummy must remain valid for as long as the
 * callback is registered (e.g. allocated globally or statically).
 *
 * @param[in,out] dummy Pointer to the dummy storage struct. Its contents are
 * overwritten.
 * @param[in]     cb    Pointer to the downlink callback function.
 * - port:  Target LoRaWAN application port.
 * - flags: Downlink frame flags (e.g. ACK, data pending).
 * - rssi:  Received Signal Strength Indicator in dBm.
 * - snr:   Signal-to-Noise Ratio in dB.
 * - len:   Payload size in bytes.
 * - data:  Pointer to the received payload (bytes buffer).
 */
void lr_lw_register_downlink_callback(
    struct lorawan_downlink_cb* dummy,
    void (*cb)(
        uint8_t port,
        uint8_t flags,
        int16_t rssi,
        int8_t snr,
        uint8_t len,
        const uint8_t* data
    )
);

/**
 * @brief Register callback to provide device battery level to the network
 * server.
 *
 * Invoked in response to device-check requests from the network server  -
 * should return the current battery status.
 *
 * @note If battery lvl callback is not set, then the backend returns 255 by
 * default on any battery checks sent by the network server.
 *
 * @param[in] cb Pointer to callback function returning current battery status:
 * - 0: External power source.
 * - 1-254: Battery level (1 = min, 254 = max).
 * - 255: Unable to measure battery level.
 */
void lr_lw_register_battery_lvl_callback(uint8_t (*cb)(void));

/**
 * @brief Register callback for datarate change notifications.
 *
 * Invoked whenever the datarate changes, whether triggered by server-side
 * Adaptive Data Rate (ADR) commands or manual configuration.
 *
 * @param[in] cb Pointer to callback function:
 * - dr: New active data rate (enum lorawan_datarate).
 */
void lr_lw_register_dr_changed_callback(void (*cb)(enum lorawan_datarate dr));

/**
 * @brief Register callback for uplink link-check command responses.
 *
 * Invoked when the network server responds to a link-check uplink request.
 *
 * @param[in] cb Pointer to callback function handling link-check responses:
 * - demod_margin: Demodulation margin in dB.
 * - nb_gateways:  Number of gateways the device is connected to (in simple
 * terms - see LoRaWAN specification for an in-depth explanation).
 */
void lr_lw_register_link_check_ans_callback(
    void (*cb)(uint8_t demod_margin, uint8_t nb_gateways)
);

/**
 * @brief Enable or disable Adaptive Data Rate (ADR).
 *
 * When enabled, the network server dynamically optimizes transmission datarate
 * and output power based on link quality (e.g. distance).
 *
 * @param[in] enabled True to enable ADR, false for static datarate management.
 */
void lr_lw_set_adr_mode(bool enabled);

/**
 * @brief Manually set the datarate.
 *
 * @note ADR must be disabled for this function to work
 *
 * @param[in] dr Datarate to set (enum lorawan_datarate).
 *
 * @return Result of the datarate set.
 * @retval LR_LW_OK                              Datarate set successfully.
 * @retval LR_LW_INVALID_DATARATE_OR_ADR_ENABLED Invalid DR value (for example
 * because of regional regulations) or ADR is currently enabled.
 */
lr_lw_result_t lr_lw_set_datarate(enum lorawan_datarate dr);
#endif
