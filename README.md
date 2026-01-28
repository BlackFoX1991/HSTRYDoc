# HstryDocu. Confidential documents. Structured. Encrypted.

Local documentation often sat unprotected on my machine. Some notes were unfinished or simply not intended for others, and in some cases they could be read, but not modified. That’s why I built an on-premise solution that provides encryption and clear permissions. Thats how HstryDocu came into my mind.

HstryDocu is an application for creating and securely encrypting documents. Within an Hstry container, multiple document blocks can be created, organized, and managed. For rendering and formatting, the established Rich Text Format (RTF) is used, ensuring content can be displayed in a structured and readable way.

The security model is based on public/private key pairs for encryption and decryption. When the application starts, the required keys can either be generated (if they do not yet exist) or imported from an existing location. External drives or other sources can also be selected; by default, HstryDocu uses the folder “HSTRY_KEY” in the root directory of the selected drive/source.

In the key management section, recipients (public keys) can be added and permissions (read/write) can be configured with fine-grained control. Changes to a container are only applied permanently after saving.


---

HstryDocu is not meant to be universal, and it’s still evolving. It exists because certain requirements can’t be met with generic tools.




