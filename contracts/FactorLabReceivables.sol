// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

contract FactorLabReceivables {
    address public owner;
    mapping(address => bool) public operators;
    mapping(bytes32 => bool) public recordedPayloads;

    enum TradeAction {
        BuyReceivable,
        SellReceivable,
        SellParticipation
    }

    event OperatorUpdated(address indexed operator, bool enabled);

    event ReceivableTradeRecorded(
        bytes32 indexed payloadHash,
        bytes32 indexed invoiceHash,
        TradeAction action,
        string invoiceNumber,
        string clientName,
        string debtor,
        string counterparty,
        string reference,
        uint256 amountMinorUnits,
        string currency,
        address indexed recordedBy,
        uint256 recordedAt
    );

    modifier onlyOwner() {
        require(msg.sender == owner, "not owner");
        _;
    }

    modifier onlyOperator() {
        require(msg.sender == owner || operators[msg.sender], "not operator");
        _;
    }

    constructor() {
        owner = msg.sender;
        operators[msg.sender] = true;
        emit OperatorUpdated(msg.sender, true);
    }

    function setOperator(address operator, bool enabled) external onlyOwner {
        operators[operator] = enabled;
        emit OperatorUpdated(operator, enabled);
    }

    function transferOwnership(address newOwner) external onlyOwner {
        require(newOwner != address(0), "zero owner");
        owner = newOwner;
        operators[newOwner] = true;
        emit OperatorUpdated(newOwner, true);
    }

    function recordTrade(
        TradeAction action,
        string calldata invoiceNumber,
        string calldata clientName,
        string calldata debtor,
        string calldata counterparty,
        string calldata reference,
        uint256 amountMinorUnits,
        string calldata currency,
        bytes32 payloadHash
    ) external onlyOperator {
        require(payloadHash != bytes32(0), "empty payload");
        require(!recordedPayloads[payloadHash], "already recorded");

        recordedPayloads[payloadHash] = true;

        emit ReceivableTradeRecorded(
            payloadHash,
            keccak256(bytes(invoiceNumber)),
            action,
            invoiceNumber,
            clientName,
            debtor,
            counterparty,
            reference,
            amountMinorUnits,
            currency,
            msg.sender,
            block.timestamp
        );
    }
}
