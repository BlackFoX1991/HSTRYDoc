# HstryDocu. Confidential documents. Structured. Encrypted.

Local documentation often sat unprotected on my machine. Some notes were unfinished or simply not intended for others, and in some cases they should be readable but not editable. That’s why I built an on-premise solution that combines strong encryption with clear, enforceable permissions: HstryDocu.

HstryDocu lets you create encrypted **containers** and manage content as independent document blocks. Each block supports **RTF** formatting for structured, readable documents, while the container maintains integrity and change tracking.

## Security & sharing
HstryDocu uses **ECC P-384** cryptography:
- Containers are **encrypted** and the header is **signed** to detect tampering.
- **Private keys are password-encrypted**.
- Sharing is done via **public keys**: recipients can be added and managed at any time.
- Optional pairing: when adding a recipient, you can attach the recipient’s **signing public key** (`.hstrysigpub`) alongside the encryption public key (`.hstrypub`) when available.

## Permissions
Recipients can be granted **per-block Read/Write access**. Permissions are enforced at block level and changes are only persisted once the container is saved.

## Key workflow
Keys can be generated or loaded from disk. Public keys can be shared safely; private keys remain encrypted with a password. External sources (e.g., USB) can be used depending on your setup.


---

HstryDocu is not meant to be universal, and it’s still evolving. It exists because certain requirements can’t be met with generic tools.

---

Launch HstryDocu and create a new container by clicking "New..."<br>
<img width="841" height="590" alt="grafik" src="https://github.com/user-attachments/assets/455849d7-3c38-4674-8f66-90f0f68a9722" /><br><br>

Configure a key pair...<br>
<img width="308" height="200" alt="grafik" src="https://github.com/user-attachments/assets/b2589f50-7300-4965-a322-e9c9df3e8406" /><br><br>
<img width="1012" height="139" alt="grafik" src="https://github.com/user-attachments/assets/0c450e85-8465-460b-9c9c-d9dc04a9f51a" /><br><br>

In Key Management, we can see our key pair. We can also add another recipient, remove one, grant read and write access to specific public keys, or create an entirely new key pair...<br>
<img width="624" height="193" alt="grafik" src="https://github.com/user-attachments/assets/1303b0cd-777e-4a35-8b3d-64839c5166c3" /><br><br>

A new block (document) can be created by right-clicking...<br>
<img width="557" height="281" alt="grafik" src="https://github.com/user-attachments/assets/dc150d48-754f-4eda-9269-7dda197c649f" /><br><br>

Block names are usually generated randomly, but you can name them however you want. Note that names can’t be duplicated within the block collection...<br>
<img width="472" height="197" alt="grafik" src="https://github.com/user-attachments/assets/34bc239f-fc32-48aa-9b56-87f3f6c5dca7" /><br>
<img width="1898" height="376" alt="grafik" src="https://github.com/user-attachments/assets/667b904a-0a7c-4a42-b467-e0ccd95a6f8e" /><br>
<img width="1918" height="446" alt="grafik" src="https://github.com/user-attachments/assets/e6c2bb85-0dfc-4a2f-89ce-5376fc0b660e" /><br>

...as we can see, you can create multiple documents in the same container.<br><br>

Over time, containers can grow larger and larger. To keep an overview of your blocks, there are search functions to look for specific text in the selected block or in the entire container...<br>
<img width="208" height="114" alt="grafik" src="https://github.com/user-attachments/assets/262d6d3c-1fd5-478b-9f0e-0cf4c8f32d14" /><br>
<img width="426" height="202" alt="grafik" src="https://github.com/user-attachments/assets/597341d4-7918-4295-bc48-82db3dc2f6d8" /><br>
<img width="994" height="318" alt="grafik" src="https://github.com/user-attachments/assets/43f0325a-6188-4f89-8e81-6823b967665e" /><br>
...by pressing F3, we can continue the search and select the next result. We can also search the entire container, and the results will be displayed as follows...<br>
<img width="771" height="457" alt="grafik" src="https://github.com/user-attachments/assets/18bda705-f5a8-427f-a5a4-2f580932fc7f" /><br>
...by double-clicking a result, HstryDocu will automatically open it and highlight the match.<br><br>

To export our documents (blocks), we can click the "Export Blocks..." menu item...<br>
<img width="193" height="217" alt="grafik" src="https://github.com/user-attachments/assets/0bdc1052-cb92-4dd5-9dd5-81c9e29802fb" /><br>
...choose the blocks you want to export, the format you need, and the output path. (Note that PDF is the only format that packs every block into a single PDF. The other two require an output folder, since multiple files will be created: 1 block = 1 file.)...<br>
<img width="680" height="408" alt="grafik" src="https://github.com/user-attachments/assets/412667d1-c848-48b0-8a37-436eaf69bc47" /><br>
















